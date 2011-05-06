=begin

= monitor.rb

Copyright (C) 2001  Shugo Maeda <shugo@ruby-lang.org>

This library is distributed under the terms of the Ruby license.
You can freely distribute/modify this library.

== example

This is a simple example.

  require 'monitor.rb'
  
  buf = []
  buf.extend(MonitorMixin)
  empty_cond = buf.new_cond
  
  # consumer
  Thread.start do
    loop do
      buf.synchronize do
        empty_cond.wait_while { buf.empty? }
        print buf.shift
      end
    end
  end
  
  # producer
  while line = ARGF.gets
    buf.synchronize do
      buf.push(line)
      empty_cond.signal
    end
  end

The consumer thread waits for the producer thread to push a line
to buf while buf.empty?, and the producer thread (main thread)
reads a line from ARGF and push it to buf, then call
empty_cond.signal.

=end
  

#
# Adds monitor functionality to an arbitrary object by mixing the module with
# +include+.  For example:
#
#    require 'monitor.rb'
#    
#    buf = []
#    buf.extend(MonitorMixin)
#    empty_cond = buf.new_cond
#    
#    # consumer
#    Thread.start do
#      loop do
#        buf.synchronize do
#          empty_cond.wait_while { buf.empty? }
#          print buf.shift
#        end
#      end
#    end
#    
#    # producer
#    while line = ARGF.gets
#      buf.synchronize do
#        buf.push(line)
#        empty_cond.signal
#      end
#    end
# 
# The consumer thread waits for the producer thread to push a line
# to buf while buf.empty?, and the producer thread (main thread)
# reads a line from ARGF and push it to buf, then call
# empty_cond.signal.
#
module MonitorMixin
  #
  # FIXME: This isn't documented in Nutshell.
  #
  # Since MonitorMixin.new_cond returns a ConditionVariable, and the example
  # above calls while_wait and signal, this class should be documented.
  #
  class ConditionVariable
    class Timeout < Exception; end
    
    # Create a new timer with the argument timeout, and add the
    # current thread to the list of waiters.  Then the thread is
    # stopped.  It will be resumed when a corresponding #signal 
    # occurs.
    def wait(timeout = nil)
      @monitor.instance_eval {mon_check_owner()}
      timer = create_timer(timeout)
      
      Thread.critical = true
      count = @monitor.instance_eval {mon_exit_for_cond()}
      @waiters.push(Thread.current)

      begin
	Thread.stop
        return true
      rescue Timeout
        return false
      ensure
	Thread.critical = true
	begin
	  if timer && timer.alive?
	    Thread.kill(timer)
	  end
	  if @waiters.include?(Thread.current)  # interrupted?
	    @waiters.delete(Thread.current)
	  end
	  @monitor.instance_eval {mon_enter_for_cond(count)}
	ensure
	  Thread.critical = false
	end
      end
    end
    

    # call #wait while the supplied block returns +true+.
    def wait_while
      while yield
	wait
      end
    end
    
    # call #wait until the supplied block returns +true+.
    def wait_until
      until yield
	wait
      end
    end
    
    # Wake up and run the next waiter
    def signal
      @monitor.instance_eval {mon_check_owner()}
      Thread.critical = true
      t = @waiters.shift
      t.wakeup if t
      Thread.critical = false
      Thread.pass
    end
    
    # Wake up all the waiters.
    def broadcast
      @monitor.instance_eval {mon_check_owner()}
      Thread.critical = true
      for t in @waiters
	t.wakeup
      end
      @waiters.clear
      Thread.critical = false
      Thread.pass
    end
    
    def count_waiters
      return @waiters.length
    end
    
    private

    def initialize(monitor)
      @monitor = monitor
      @waiters = []
    end

    def create_timer(timeout)
      if timeout
	waiter = Thread.current
	return Thread.start {
	  Thread.pass
	  sleep(timeout)
	  Thread.critical = true
	  waiter.raise(Timeout.new)
	}
      else
        return nil
      end
    end
  end
  
  def self.extend_object(obj)
    super(obj)
    obj.instance_eval {mon_initialize()}
  end
  
  #
  # Attempts to enter exclusive section.  Returns +false+ if lock fails.
  #
  def mon_try_enter
    result = false
    Thread.critical = true
    if @mon_owner.nil?
      @mon_owner = Thread.current
    end
    if @mon_owner == Thread.current
      @mon_count += 1
      result = true
    end
    Thread.critical = false
    return result
  end
  # For backward compatibility
  alias try_mon_enter mon_try_enter

  #
  # Enters exclusive section.
  #
  def mon_enter
    Thread.critical = true
    mon_acquire(@mon_entering_queue)
    @mon_count += 1
  ensure
    Thread.critical = false
  end
  
  #
  # Leaves exclusive section.
  #
  def mon_exit
    mon_check_owner
    Thread.critical = true
    @mon_count -= 1
    if @mon_count == 0
      mon_release
    end
    Thread.critical = false
    Thread.pass
  end

  #
  # Enters exclusive section and executes the block.  Leaves the exclusive
  # section automatically when the block exits.  See example under
  # +MonitorMixin+.
  #
  def mon_synchronize
    mon_enter
    begin
      yield
    ensure
      mon_exit
    end
  end
  alias synchronize mon_synchronize
  
  #
  # FIXME: This isn't documented in Nutshell.
  # 
  # Create a new condition variable for this monitor.
  # This facilitates control of the monitor with #signal and #wait.
  #
  def new_cond
    return ConditionVariable.new(self)
  end

  private

  def initialize(*args)
    super
    mon_initialize
  end

  # called by initialize method to set defaults for instance variables.
  def mon_initialize
    @mon_owner = nil
    @mon_count = 0
    @mon_entering_queue = []
    @mon_waiting_queue = []
  end

  # Throw a ThreadError exception if the current thread
  # does't own the monitor
  def mon_check_owner
    if @mon_owner != Thread.current
      raise ThreadError, "current thread not owner"
    end
  end

  def mon_acquire(queue)
    while @mon_owner && @mon_owner != Thread.current
      queue.push(Thread.current)
      Thread.stop
      Thread.critical = true
    end
    @mon_owner = Thread.current
  end

  def mon_release
    @mon_owner = nil
    t = @mon_waiting_queue.shift
    t = @mon_entering_queue.shift unless t
    t.wakeup if t
  end

  def mon_enter_for_cond(count)
    mon_acquire(@mon_waiting_queue)
    @mon_count = count
  end

  def mon_exit_for_cond
    count = @mon_count
    @mon_count = 0
    return count
  ensure
    mon_release
  end
end

# Monitors provide means of mutual exclusion for Thread programming.
# A critical region is created by means of the synchronize method,
# which takes a block.
# The condition variables (created with #new_cond) may be used 
# to control the execution of a monitor with #signal and #wait.
#
# the Monitor class wraps MonitorMixin, and provides aliases
#  alias try_enter try_mon_enter
#  alias enter mon_enter
#  alias exit mon_exit
# to access its methods more concisely.
class Monitor
  include MonitorMixin
  alias try_enter try_mon_enter
  alias enter mon_enter
  alias exit mon_exit
end


# Documentation comments:
#  - All documentation comes from Nutshell.
#  - MonitorMixin.new_cond appears in the example, but is not documented in
#    Nutshell.
#  - All the internals (internal modules Accessible and Initializable, class
#    ConditionVariable) appear in RDoc.  It might be good to hide them, by
#    making them private, or marking them :nodoc:, etc.
#  - The entire example from the RD section at the top is replicated in the RDoc
#    comment for MonitorMixin.  Does the RD section need to remain?
#  - RDoc doesn't recognise aliases, so we have mon_synchronize documented, but
#    not synchronize.
#  - mon_owner is in Nutshell, but appears as an accessor in a separate module
#    here, so is hard/impossible to RDoc.  Some other useful accessors
#    (mon_count and some queue stuff) are also in this module, and don't appear
#    directly in the RDoc output.
#  - in short, it may be worth changing the code layout in this file to make the
#    documentation easier

# Local variables:
# mode: Ruby
# tab-width: 8
# End:
