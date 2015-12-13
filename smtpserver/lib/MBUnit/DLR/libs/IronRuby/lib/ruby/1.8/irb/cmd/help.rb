#
#   help.rb - helper using ri
#   	$Release Version: 0.9.5$
#   	$Revision: 11708 $
#   	$Date: 2007-02-13 08:01:19 +0900 (Tue, 13 Feb 2007) $
#
# --
#
#   
#

require 'rdoc/ri/ri_driver'

module IRB
  module ExtendCommand
    module Help
      begin
        @ri = RiDriver.new
      rescue SystemExit
      else
        def self.execute(context, *names)
          names.each do |name|
            begin
              @ri.get_info_for(name.to_s)
            rescue RiError
              puts $!.message
            end
          end
          nil
        end
      end
    end
  end
end
