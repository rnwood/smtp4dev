# WSDL4R - XMLSchema schema definition for WSDL.
# Copyright (C) 2002, 2003-2005  NAKAMURA, Hiroshi <nahi@ruby-lang.org>.

# This program is copyrighted free software by NAKAMURA, Hiroshi.  You can
# redistribute it and/or modify it under the same terms of Ruby's license;
# either the dual license version in 2003, or any later version.


require 'wsdl/info'
require 'xsd/namedelements'


module WSDL
module XMLSchema


class Schema < Info
  attr_reader :targetnamespace	# required
  attr_reader :complextypes
  attr_reader :simpletypes
  attr_reader :elements
  attr_reader :attributes
  attr_reader :imports
  attr_accessor :attributeformdefault
  attr_accessor :elementformdefault

  attr_reader :importedschema

  def initialize
    super
    @targetnamespace = nil
    @complextypes = XSD::NamedElements.new
    @simpletypes = XSD::NamedElements.new
    @elements = XSD::NamedElements.new
    @attributes = XSD::NamedElements.new
    @imports = []
    @attributeformdefault = "unqualified"
    @elementformdefault = "unqualified"
    @importedschema = {}
    @location = nil
    @root = self
  end

  def location
    @location || (root.nil? ? nil : root.location)
  end

  def location=(location)
    @location = location
  end

  def parse_element(element)
    case element
    when ImportName
      o = Import.new
      @imports << o
      o
    when IncludeName
      o = Include.new
      @imports << o
      o
    when ComplexTypeName
      o = ComplexType.new
      @complextypes << o
      o
    when SimpleTypeName
      o = SimpleType.new
      @simpletypes << o
      o
    when ElementName
      o = Element.new
      @elements << o
      o
    when AttributeName
      o = Attribute.new
      @attributes << o
      o
    else
      nil
    end
  end

  def parse_attr(attr, value)
    case attr
    when TargetNamespaceAttrName
      @targetnamespace = value.source
    when AttributeFormDefaultAttrName
      @attributeformdefault = value.source
    when ElementFormDefaultAttrName
      @elementformdefault = value.source
    else
      nil
    end
  end

  def collect_attributes
    result = XSD::NamedElements.new
    result.concat(@attributes)
    @imports.each do |import|
      result.concat(import.content.collect_attributes) if import.content
    end
    result
  end

  def collect_elements
    result = XSD::NamedElements.new
    result.concat(@elements)
    @imports.each do |import|
      result.concat(import.content.collect_elements) if import.content
    end
    result
  end

  def collect_complextypes
    result = XSD::NamedElements.new
    result.concat(@complextypes)
    @imports.each do |import|
      result.concat(import.content.collect_complextypes) if import.content
    end
    result
  end

  def collect_simpletypes
    result = XSD::NamedElements.new
    result.concat(@simpletypes)
    @imports.each do |import|
      result.concat(import.content.collect_simpletypes) if import.content
    end
    result
  end

  def self.parse_element(element)
    if element == SchemaName
      Schema.new
    else
      nil
    end
  end
end


end
end
