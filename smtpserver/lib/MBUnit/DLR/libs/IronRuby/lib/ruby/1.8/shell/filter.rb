#
#   shell/filter.rb - 
#   	$Release Version: 0.6.0 $
#   	$Revision: 11708 $
#   	$Date: 2007-02-13 08:01:19 +0900 (Tue, 13 Feb 2007) $
#   	by Keiju ISHITSUKA(Nihon Rational Software Co.,Ltd)
#
# --
#
#   
#

class Shell
  #
  # Filter
  # A method to require
  #    each()
  #
  class Filter
    include Enumerable

    def initialize(sh)
      @shell = sh	  # parent shell
      @input = nil	  # input filter
    end

    attr_reader :input

    def input=(filter)
      @input = filter
    end
    
    def each(rs = nil)
      rs = @shell.record_separator unless rs
      if @input
	@input.each(rs){|l| yield l}
      end
    end

    def < (src)
      case src
      when String
	cat = Cat.new(@shell, src)
	cat | self
      when IO
	self.input = src
	self
      else
	Shell.Fail Error::CantApplyMethod, "<", to.class
      end
    end

    def > (to)
      case to
      when String
	dst = @shell.open(to, "w")
	begin
	  each(){|l| dst << l}
	ensure
	  dst.close
	end
      when IO
	each(){|l| to << l}
      else
	Shell.Fail Error::CantApplyMethod, ">", to.class
      end
      self
    end

    def >> (to)
      begin
	Shell.cd(@shell.pwd).append(to, self)
      rescue CantApplyMethod
	Shell.Fail Error::CantApplyMethod, ">>", to.class
      end
    end

    def | (filter)
      filter.input = self
      if active?
	@shell.process_controller.start_job filter
      end
      filter
    end

    def + (filter)
      Join.new(@shell, self, filter)
    end

    def to_a
      ary = []
      each(){|l| ary.push l}
      ary
    end

    def to_s
      str = ""
      each(){|l| str.concat l}
      str
    end

    def inspect
      if @shell.debug.kind_of?(Integer) && @shell.debug > 2
	super
      else
	to_s
      end
    end
  end
end
