#
#   irb/lc/ja/error.rb - 
#   	$Release Version: 0.9.5$
#   	$Revision: 11708 $
#   	$Date: 2007-02-13 08:01:19 +0900 (Tue, 13 Feb 2007) $
#   	by Keiju ISHITSUKA(keiju@ruby-lang.org)
#
# --
#
#   
#
require "e2mmap"

module IRB
  # exceptions
  extend Exception2MessageMapper
  def_exception :UnrecognizedSwitch, '$B%9%$%C%A(B(%s)$B$,J,$j$^$;$s(B'
  def_exception :NotImplementedError, '`%s\'$B$NDj5A$,I,MW$G$9(B'
  def_exception :CantReturnToNormalMode, 'Normal$B%b!<%I$KLa$l$^$;$s(B.'
  def_exception :IllegalParameter, '$B%Q%i%a!<%?(B(%s)$B$,4V0c$C$F$$$^$9(B.'
  def_exception :IrbAlreadyDead, 'Irb$B$O4{$K;`$s$G$$$^$9(B.'
  def_exception :IrbSwitchedToCurrentThread, '$B%+%l%s%H%9%l%C%I$K@Z$jBX$o$j$^$7$?(B.'
  def_exception :NoSuchJob, '$B$=$N$h$&$J%8%g%V(B(%s)$B$O$"$j$^$;$s(B.'
  def_exception :CantShiftToMultiIrbMode, 'multi-irb mode$B$K0\$l$^$;$s(B.'
  def_exception :CantChangeBinding, '$B%P%$%s%G%#%s%0(B(%s)$B$KJQ99$G$-$^$;$s(B.'
  def_exception :UndefinedPromptMode, '$B%W%m%s%W%H%b!<%I(B(%s)$B$ODj5A$5$l$F$$$^$;$s(B.'
end
