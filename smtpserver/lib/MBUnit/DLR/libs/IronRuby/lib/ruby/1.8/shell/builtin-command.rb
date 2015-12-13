#
#   shell/builtin-command.rb - 
#   	$Release Version: 0.6.0 $
#   	$Revision: 11708 $
#   	$Date: 2007-02-13 08:01:19 +0900 (Tue, 13 Feb 2007) $
#   	by Keiju ISHITSUKA(Nihon Rational Software Co.,Ltd)
#
# --
#
#   
#

require "shell/filter"

class Shell
  class BuiltInCommand<Filter
    def wait?
      false
    end
    def active?
      true
    end
  end

  class Echo < BuiltInCommand
    def initialize(sh, *strings)
      super sh
      @strings = strings
    end
    
    def each(rs = nil)
      rs =  @shell.record_separator unless rs
      for str  in @strings
	yield str + rs
      end
    end
  end

  class Cat < BuiltInCommand
    def initialize(sh, *filenames)
      super sh
      @cat_files = filenames
    end

    def each(rs = nil)
      if @cat_files.empty?
	super
      else
	for src in @cat_files
	  @shell.foreach(src, rs){|l| yield l}
	end
      end
    end
  end

  class Glob < BuiltInCommand
    def initialize(sh, pattern)
      super sh

      @pattern = pattern
      Thread.critical = true
      back = Dir.pwd
      begin
	Dir.chdir @shell.cwd
	@files = Dir[pattern]
      ensure
	Dir.chdir back
	Thread.critical = false
      end
    end

    def each(rs = nil)
      rs =  @shell.record_separator unless rs
      for f  in @files
	yield f+rs
      end
    end
  end

#   class Sort < Cat
#     def initialize(sh, *filenames)
#       super
#     end
#
#     def each(rs = nil)
#       ary = []
#       super{|l|	ary.push l}
#       for l in ary.sort!
# 	yield l
#       end
#     end
#   end

  class AppendIO < BuiltInCommand
    def initialize(sh, io, filter)
      super sh
      @input = filter
      @io = io
    end

    def input=(filter)
      @input.input=filter
      for l in @input
	@io << l
      end
    end

  end

  class AppendFile < AppendIO
    def initialize(sh, to_filename, filter)
      @file_name = to_filename
      io = sh.open(to_filename, "a")
      super(sh, io, filter)
    end

    def input=(filter)
      begin
	super
      ensure
	@io.close
      end
    end
  end

  class Tee < BuiltInCommand
    def initialize(sh, filename)
      super sh
      @to_filename = filename
    end

    def each(rs = nil)
      to = @shell.open(@to_filename, "w")
      begin
	super{|l| to << l; yield l}
      ensure
	to.close
      end
    end
  end

  class Concat < BuiltInCommand
    def initialize(sh, *jobs)
      super(sh)
      @jobs = jobs
    end

    def each(rs = nil)
      while job = @jobs.shift
	job.each{|l| yield l}
      end
    end
  end
end
