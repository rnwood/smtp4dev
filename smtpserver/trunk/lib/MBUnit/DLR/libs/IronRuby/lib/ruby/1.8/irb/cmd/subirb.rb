#!/usr/local/bin/ruby
#
#   multi.rb - 
#   	$Release Version: 0.9.5$
#   	$Revision: 11708 $
#   	$Date: 2007-02-13 08:01:19 +0900 (Tue, 13 Feb 2007) $
#   	by Keiju ISHITSUKA(keiju@ruby-lang.org)
#
# --
#
#   
#

require "irb/cmd/nop.rb"
require "irb/ext/multi-irb"

module IRB
  module ExtendCommand
    class IrbCommand<Nop
      def execute(*obj)
	IRB.irb(nil, *obj)
      end
    end

    class Jobs<Nop
      def execute
	IRB.JobManager
      end
    end

    class Foreground<Nop
      def execute(key)
	IRB.JobManager.switch(key)
      end
    end

    class Kill<Nop
      def execute(*keys)
	IRB.JobManager.kill(*keys)
      end
    end
  end
end
