#
#   shell/error.rb - 
#   	$Release Version: 0.6.0 $
#   	$Revision: 11708 $
#   	$Date: 2007-02-13 08:01:19 +0900 (Tue, 13 Feb 2007) $
#   	by Keiju ISHITSUKA(Nihon Rational Software Co.,Ltd)
#
# --
#
#   
#

require "e2mmap"

class Shell
  module Error
    extend Exception2MessageMapper
    def_e2message TypeError, "wrong argument type %s (expected %s)"

    def_exception :DirStackEmpty, "Directory stack empty."
    def_exception :CantDefine, "Can't define method(%s, %s)."
    def_exception :CantApplyMethod, "This method(%s) does not apply to this type(%s)."
    def_exception :CommandNotFound, "Command not found(%s)."
  end
end

