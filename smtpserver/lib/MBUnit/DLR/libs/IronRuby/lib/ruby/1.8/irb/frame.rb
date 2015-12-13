#
#   frame.rb - 
#   	$Release Version: 0.9$
#   	$Revision: 11708 $
#   	$Date: 2007-02-13 08:01:19 +0900 (Tue, 13 Feb 2007) $
#   	by Keiju ISHITSUKA(Nihon Rational Software Co.,Ltd)
#
# --
#
#   
#

require "e2mmap"

module IRB
  class Frame
    extend Exception2MessageMapper
    def_exception :FrameOverflow, "frame overflow"
    def_exception :FrameUnderflow, "frame underflow"

    INIT_STACK_TIMES = 3
    CALL_STACK_OFFSET = 3

    def initialize
      @frames = [TOPLEVEL_BINDING] * INIT_STACK_TIMES
    end

    def trace_func(event, file, line, id, binding)
      case event
      when 'call', 'class'
	@frames.push binding
      when 'return', 'end'
	@frames.pop
      end
    end

    def top(n = 0)
      bind = @frames[-(n + CALL_STACK_OFFSET)]
      Fail FrameUnderflow unless bind
      bind
    end

    def bottom(n = 0)
      bind = @frames[n]
      Fail FrameOverflow unless bind
      bind
    end

    # singleton functions
    def Frame.bottom(n = 0)
      @backtrace.bottom(n)
    end

    def Frame.top(n = 0)
      @backtrace.top(n)
    end

    def Frame.sender
      eval "self", @backtrace.top
    end

    @backtrace = Frame.new
    set_trace_func proc{|event, file, line, id, binding, klass|
      @backtrace.trace_func(event, file, line, id, binding)
    }
  end
end
