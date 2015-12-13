# ****************************************************************************
#
# Copyright (c) Microsoft Corporation. 
#
# This source code is subject to terms and conditions of the Microsoft Public License. A 
# copy of the license can be found in the License.html file at the root of this distribution. If 
# you cannot locate the  Microsoft Public License, please send an email to 
# ironruby@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
# by the terms of the Microsoft Public License.
#
# You must not remove this notice, or any other, from this software.
#
#
# ****************************************************************************

abort "*** Sorry, ParseTree doesn't work with ruby #{RUBY_VERSION}" if
  RUBY_VERSION >= "1.9"

load_assembly 'IronRuby.Libraries', 'IronRuby.StandardLibrary.ParseTree'

class Module
  def modules
    ancestors[1..-1]
  end
end

class Class
  def modules
    a = self.ancestors
    a[1..a.index(superclass)-1]
  end
end

##
# ParseTree is a RubyInline-style extension that accesses and
# traverses the internal parse tree created by ruby.
#
#   class Example
#     def blah
#       return 1 + 1
#     end
#   end
#
#   ParseTree.new.parse_tree(Example)
#   => [[:class, :Example, :Object,
#          [:defn,
#            "blah",
#            [:scope,
#              [:block,
#                [:args],
#                [:return, [:call, [:lit, 1], "+", [:array, [:lit, 1]]]]]]]]]

class ParseTree

  VERSION = '2.1.1'

  ##
  # Front end translation method.

  def self.translate(klass_or_str, method=nil)
    pt = self.new(false)
    case klass_or_str
    when String then
      sexp = pt.parse_tree_for_string(klass_or_str).first
      if method then
        # class, scope, block, *methods
        sexp.last.last[1..-1].find do |defn|
          defn[1] == method
        end
      else
        sexp
      end
    else
      unless method.nil? then
        if method.to_s =~ /^self\./ then
          method = method.to_s[5..-1].intern
          pt.parse_tree_for_method(klass_or_str, method, true)
        else
          pt.parse_tree_for_method(klass_or_str, method)
        end
      else
        pt.parse_tree(klass_or_str).first
      end
    end
  end

  ##
  # Initializes a ParseTree instance. Includes newline nodes if
  # +include_newlines+ which defaults to +$DEBUG+.

  def initialize(include_newlines=$DEBUG)
    @include_newlines = include_newlines
  end

  ##
  # Main driver for ParseTree. Returns an array of arrays containing
  # the parse tree for +klasses+.
  #
  # Structure:
  #
  #   [[:class, classname, superclassname, [:defn :method1, ...], ...], ...]
  #
  # NOTE: v1.0 - v1.1 had the signature (klass, meth=nil). This wasn't
  # used much at all and since parse_tree_for_method already existed,
  # it was deemed more useful to expand this method to do multiple
  # classes.

  def parse_tree(*klasses)
    result = []
    klasses.each do |klass|
      klassname = klass.name rescue '' # HACK klass.name should never be nil
                                   # Tempfile's DelegateClass(File) seems to
                                   # cause this
      klassname = "UnnamedClass_#{klass.object_id}" if klassname.empty?
      klassname = klassname.to_sym

      code = if Class === klass then
               sc = klass.superclass
               sc_name = ((sc.nil? or sc.name.empty?) ? "nil" : sc.name).intern
               [:class, klassname, [:const, sc_name]]
             else
               [:module, klassname]
             end

      method_names = []
      method_names += klass.instance_methods false
      method_names += klass.private_instance_methods false
      # protected methods are included in instance_methods, go figure!

      method_names.sort.each do |m|
        r = parse_tree_for_method(klass, m.to_sym)
        code << r
      end

      klass.modules.each do |mod| # TODO: add a test for this 
        mod.instance_methods.each do |m|
          r = parse_tree_for_method(mod, m.to_sym)
          code << r
        end
      end

      klass.singleton_methods(false).sort.each do |m|
        code << parse_tree_for_method(klass, m.to_sym, true)
      end

      result << code
    end
    return result
  end

  ##
  # Returns the parse tree for just one +method+ of a class +klass+.
  #
  # Format:
  #
  #   [:defn, :name, :body]

  def parse_tree_for_method(klass, method, is_cls_meth=false)
    $stderr.puts "** parse_tree_for_method(#{klass}, #{method}):" if $DEBUG
    r = parse_tree_for_meth(klass, method.to_sym, is_cls_meth)
    r
  end

  ##
  # Returns the parse tree for a string +source+.
  #
  # Format:
  #
  #   [[sexps] ... ]

  def parse_tree_for_string(source, filename = '(string)', line = 1)
    old_verbose, $VERBOSE = $VERBOSE, true
    return parse_tree_for_str0(source, filename, line)
  ensure
    $VERBOSE = old_verbose
  end

  def parse_tree_for_str0(*__1args2__) # :nodoc:
    parse_tree_for_str(*__1args2__)    # just helps clean up the binding
  end

  if RUBY_VERSION < "1.8.4" then
    inline do |builder|
      builder.add_type_converter("bool", '', '')
      builder.c_singleton "
        bool has_alloca() {
          (void)self;
          #ifdef C_ALLOCA
            return Qtrue;
          #else
            return Qfalse;
          #endif
          }"
    end
  else
    def self.has_alloca
      true
    end
  end


  NODE_NAMES = [
                #  00
                :method, :fbody, :cfunc, :scope, :block,
                :if, :case, :when, :opt_n, :while,
                #  10
                :until, :iter, :for, :break, :next,
                :redo, :retry, :begin, :rescue, :resbody,
                #  20
                :ensure, :and, :or, :not, :masgn,
                :lasgn, :dasgn, :dasgn_curr, :gasgn, :iasgn,
                #  30
                :cdecl, :cvasgn, :cvdecl, :op_asgn1, :op_asgn2,
                :op_asgn_and, :op_asgn_or, :call, :fcall, :vcall,
                #  40
                :super, :zsuper, :array, :zarray, :hash,
                :return, :yield, :lvar, :dvar, :gvar,
                #  50
                :ivar, :const, :cvar, :nth_ref, :back_ref,
                :match, :match2, :match3, :lit, :str,
                #  60
                :dstr, :xstr, :dxstr, :evstr, :dregx,
                :dregx_once, :args, :argscat, :argspush, :splat,
                #  70
                :to_ary, :svalue, :block_arg, :block_pass, :defn,
                :defs, :alias, :valias, :undef, :class,
                #  80
                :module, :sclass, :colon2, :colon3, :cref,
                :dot2, :dot3, :flip2, :flip3, :attrset,
                #  90
                :self, :nil, :true, :false, :defined,
                #  95
                :newline, :postexe, :alloca, :dmethod, :bmethod,
                # 100
                :memo, :ifunc, :dsym, :attrasgn,
                :last
               ]

  if RUBY_VERSION > "1.9" then
    NODE_NAMES.insert NODE_NAMES.index(:hash), :values
    NODE_NAMES.insert NODE_NAMES.index(:defined), :errinfo
    NODE_NAMES.insert NODE_NAMES.index(:last), :prelude, :lambda
    NODE_NAMES.delete :dmethod
    NODE_NAMES[128] = NODE_NAMES.delete :newline
  end

  ############################################################
  # END of rdoc methods
  ############################################################

  include ::IronRuby::ParseTree

end # ParseTree class
