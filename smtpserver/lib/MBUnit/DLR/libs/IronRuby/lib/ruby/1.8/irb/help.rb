#
#   irb/help.rb - print usase module
#   	$Release Version: 0.9.5$
#   	$Revision: 11708 $
#   	$Date: 2007-02-13 08:01:19 +0900 (Tue, 13 Feb 2007) $
#   	by Keiju ISHITSUKA(keiju@ishitsuka.com)
#
# --
#
#   
#

module IRB
  def IRB.print_usage
    lc = IRB.conf[:LC_MESSAGES]
    path = lc.find("irb/help-message")
    space_line = false
    File.foreach(path) do
      |l|
      if /^\s*$/ =~ l
	lc.puts l unless space_line
	space_line = true
	next
      end
      space_line = false
      
      l.sub!(/#.*$/, "")
      next if /^\s*$/ =~ l
      lc.puts l
    end
  end
end

