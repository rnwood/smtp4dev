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

# In non-iirb sessions, TOPLEVEL_BINDING isn't defined
unless defined?(TOPLEVEL_BINDING)
  TOPLEVEL_BINDING = binding
end

# IronRuby bug: IO#read seems to chop off the first char

class TCPSocket
  def read size
    recv size
  end
end

# Subclass Tracking

module SubclassTracking
  class Holder < Array
    def each_object(klass, &b)
      self.each { |c| b[c] }
    end
  end
  def self.extended(klass)
    (class << klass; self; end).send :attr_accessor, :subclasses
    (class << klass; self; end).send :define_method, :inherited do |klazz|
      klass.subclasses << klazz
      super
    end
    klass.subclasses = Holder.new
  end
end

class IO
  def flush; end # nop
end

class String
  def hex
    self.to_i(16)
  end
end
