# 
# = un.rb
# 
# Copyright (c) 2003 WATANABE Hirofumi <eban@ruby-lang.org>
# 
# This program is free software.
# You can distribute/modify this program under the same terms of Ruby.
# 
# == Utilities to replace common UNIX commands in Makefiles etc
#
# == SYNOPSIS
#
#   ruby -run -e cp -- [OPTION] SOURCE DEST
#   ruby -run -e ln -- [OPTION] TARGET LINK_NAME
#   ruby -run -e mv -- [OPTION] SOURCE DEST
#   ruby -run -e rm -- [OPTION] FILE
#   ruby -run -e mkdir -- [OPTION] DIRS
#   ruby -run -e rmdir -- [OPTION] DIRS
#   ruby -run -e install -- [OPTION] SOURCE DEST
#   ruby -run -e chmod -- [OPTION] OCTAL-MODE FILE
#   ruby -run -e touch -- [OPTION] FILE
#   ruby -run -e help [COMMAND]

require "fileutils"
require "optparse"

module FileUtils
#  @fileutils_label = ""
  @fileutils_output = $stdout
end

def setup(options = "")
  ARGV.map! do |x|
    case x
    when /^-/
      x.delete "^-#{options}v"
    when /[*?\[{]/
      Dir[x]
    else
      x
    end
  end
  ARGV.flatten!
  ARGV.delete_if{|x| x == "-"}
  opt_hash = {}
  OptionParser.new do |o|
    options.scan(/.:?/) do |s|
      o.on("-" + s.tr(":", " ")) do |val|
        opt_hash[s.delete(":").intern] = val
      end
    end
    o.on("-v") do opt_hash[:verbose] = true end
    o.parse!
  end
  yield ARGV, opt_hash
end

##
# Copy SOURCE to DEST, or multiple SOURCE(s) to DIRECTORY
#
#   ruby -run -e cp -- [OPTION] SOURCE DEST
#
#   -p		preserve file attributes if possible
#   -r		copy recursively
#   -v		verbose
#

def cp
  setup("pr") do |argv, options|
    cmd = "cp"
    cmd += "_r" if options.delete :r
    options[:preserve] = true if options.delete :p
    dest = argv.pop
    argv = argv[0] if argv.size == 1
    FileUtils.send cmd, argv, dest, options
  end
end

##
# Create a link to the specified TARGET with LINK_NAME.
#
#   ruby -run -e ln -- [OPTION] TARGET LINK_NAME
#
#   -s		make symbolic links instead of hard links
#   -f		remove existing destination files
#   -v		verbose
#

def ln
  setup("sf") do |argv, options|
    cmd = "ln"
    cmd += "_s" if options.delete :s
    options[:force] = true if options.delete :f
    dest = argv.pop
    argv = argv[0] if argv.size == 1
    FileUtils.send cmd, argv, dest, options
  end
end

##
# Rename SOURCE to DEST, or move SOURCE(s) to DIRECTORY.
#
#   ruby -run -e mv -- [OPTION] SOURCE DEST
#
#   -v		verbose
#

def mv
  setup do |argv, options|
    dest = argv.pop
    argv = argv[0] if argv.size == 1
    FileUtils.mv argv, dest, options
  end
end

##
# Remove the FILE
#
#   ruby -run -e rm -- [OPTION] FILE
#
#   -f		ignore nonexistent files
#   -r		remove the contents of directories recursively
#   -v		verbose
#

def rm
  setup("fr") do |argv, options|
    cmd = "rm"
    cmd += "_r" if options.delete :r
    options[:force] = true if options.delete :f
    FileUtils.send cmd, argv, options
  end
end

##
# Create the DIR, if they do not already exist.
#
#   ruby -run -e mkdir -- [OPTION] DIR
#
#   -p		no error if existing, make parent directories as needed
#   -v		verbose
#

def mkdir
  setup("p") do |argv, options|
    cmd = "mkdir"
    cmd += "_p" if options.delete :p
    FileUtils.send cmd, argv, options
  end
end

##
# Remove the DIR.
#
#   ruby -run -e rmdir -- [OPTION] DIR
#
#   -v		verbose
#

def rmdir
  setup do |argv, options|
    FileUtils.rmdir argv, options
  end
end

##
# Copy SOURCE to DEST.
#
#   ruby -run -e install -- [OPTION] SOURCE DEST
#
#   -p		apply access/modification times of SOURCE files to
#  		corresponding destination files
#   -m		set permission mode (as in chmod), instead of 0755
#   -v		verbose
#

def install
  setup("pm:") do |argv, options|
    options[:mode] = (mode = options.delete :m) ? mode.oct : 0755
    options[:preserve] = true if options.delete :p
    dest = argv.pop
    argv = argv[0] if argv.size == 1
    FileUtils.install argv, dest, options
  end
end

##
# Change the mode of each FILE to OCTAL-MODE.
#
#   ruby -run -e chmod -- [OPTION] OCTAL-MODE FILE
#
#   -v		verbose
#

def chmod
  setup do |argv, options|
    mode = argv.shift.oct
    FileUtils.chmod mode, argv, options
  end
end

##
# Update the access and modification times of each FILE to the current time.
#
#   ruby -run -e touch -- [OPTION] FILE
#
#   -v		verbose
#

def touch
  setup do |argv, options|
    FileUtils.touch argv, options
  end
end

##
# Display help message.
#
#   ruby -run -e help [COMMAND]
#

def help
  setup do |argv,|
    all = argv.empty?
    open(__FILE__) do |me|
      while me.gets("##\n")
	if help = me.gets("\n\n")
	  if all or argv.delete help[/-e \w+/].sub(/-e /, "")
	    print help.gsub(/^# ?/, "")
	  end
	end
      end
    end
  end
end
