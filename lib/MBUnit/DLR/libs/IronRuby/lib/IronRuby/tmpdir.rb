#
# tmpdir - retrieve temporary directory path
#
# $Id: tmpdir.rb 11708 2007-02-12 23:01:19Z shyouhei $
#

require 'mscorlib'

class Dir

  begin
    @@systmpdir = System::IO::Path.get_temp_path
  rescue
    @@systmpdir = nil
  end

  private

  ##
  # Returns the operating system's temporary file path.

  def Dir::tmpdir
    tmp = '.'
    if $SAFE > 0 and @@systmpdir
      tmp = @@systmpdir
    else
      for dir in [ENV['TMPDIR'], ENV['TMP'], ENV['TEMP'],
	          ENV['USERPROFILE'], @@systmpdir, '/tmp']
	if dir and File.directory?(dir) and File.writable?(dir)
	  tmp = dir
	  break
	end
      end
    end
    File.expand_path(tmp)
  end
end

