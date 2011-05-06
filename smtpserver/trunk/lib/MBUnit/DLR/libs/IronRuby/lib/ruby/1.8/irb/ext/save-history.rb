#!/usr/local/bin/ruby
#
#   save-history.rb - 
#   	$Release Version: 0.9.5$
#   	$Revision: 11708 $
#   	$Date: 2007-02-13 08:01:19 +0900 (Tue, 13 Feb 2007) $
#   	by Keiju ISHITSUKAkeiju@ruby-lang.org)
#
# --
#
#   
#

require "readline"

module IRB
  module HistorySavingAbility
    @RCS_ID='-$Id: save-history.rb 11708 2007-02-12 23:01:19Z shyouhei $-'
  end

  class Context
    def init_save_history
      unless (class<<@io;self;end).include?(HistorySavingAbility)
	@io.extend(HistorySavingAbility)
      end
    end

    def save_history
      IRB.conf[:SAVE_HISTORY]
    end

    def save_history=(val)
      IRB.conf[:SAVE_HISTORY] = val
      if val
	main_context = IRB.conf[:MAIN_CONTEXT]
	main_context = self unless main_context
	main_context.init_save_history
      end
    end

    def history_file
      IRB.conf[:HISTORY_FILE]
    end

    def history_file=(hist)
      IRB.conf[:HISTORY_FILE] = hist
    end
  end

  module HistorySavingAbility
    include Readline

    def HistorySavingAbility.create_finalizer
      proc do
	if num = IRB.conf[:SAVE_HISTORY] and (num = num.to_i) > 0
	  if hf = IRB.conf[:HISTORY_FILE]
	    file = File.expand_path(hf)
	  end
	  file = IRB.rc_file("_history") unless file
	  open(file, 'w' ) do |f|
	    hist = HISTORY.to_a
	    f.puts(hist[-num..-1] || hist)
	  end
	end
      end
    end

    def HistorySavingAbility.extended(obj)
      ObjectSpace.define_finalizer(obj, HistorySavingAbility.create_finalizer)
      obj.load_history
      obj
    end

    def load_history
      hist = IRB.conf[:HISTORY_FILE]
      hist = IRB.rc_file("_history") unless hist
      if File.exist?(hist)
	open(hist) do |f|
	  f.each {|l| HISTORY << l.chomp}
	end
      end
    end
  end
end

