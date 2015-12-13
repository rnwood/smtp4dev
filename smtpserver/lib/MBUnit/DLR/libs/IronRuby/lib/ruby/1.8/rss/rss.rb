require "time"

class Time
  class << self
    unless respond_to?(:w3cdtf)
      def w3cdtf(date)
        if /\A\s*
            (-?\d+)-(\d\d)-(\d\d)
            (?:T
            (\d\d):(\d\d)(?::(\d\d))?
            (\.\d+)?
            (Z|[+-]\d\d:\d\d)?)?
            \s*\z/ix =~ date and (($5 and $8) or (!$5 and !$8))
          datetime = [$1.to_i, $2.to_i, $3.to_i, $4.to_i, $5.to_i, $6.to_i] 
          datetime << $7.to_f * 1000000 if $7
          if $8
            Time.utc(*datetime) - zone_offset($8)
          else
            Time.local(*datetime)
          end
        else
          raise ArgumentError.new("invalid date: #{date.inspect}")
        end
      end
    end
  end

  unless instance_methods.include?("w3cdtf")
    alias w3cdtf iso8601
  end
end

require "English"
require "rss/utils"
require "rss/converter"
require "rss/xml-stylesheet"

module RSS

  VERSION = "0.1.6"

  URI = "http://purl.org/rss/1.0/"

  DEBUG = false

  class Error < StandardError; end

  class OverlappedPrefixError < Error
    attr_reader :prefix
    def initialize(prefix)
      @prefix = prefix
    end
  end

  class InvalidRSSError < Error; end

  class MissingTagError < InvalidRSSError
    attr_reader :tag, :parent
    def initialize(tag, parent)
      @tag, @parent = tag, parent
      super("tag <#{tag}> is missing in tag <#{parent}>")
    end
  end

  class TooMuchTagError < InvalidRSSError
    attr_reader :tag, :parent
    def initialize(tag, parent)
      @tag, @parent = tag, parent
      super("tag <#{tag}> is too much in tag <#{parent}>")
    end
  end

  class MissingAttributeError < InvalidRSSError
    attr_reader :tag, :attribute
    def initialize(tag, attribute)
      @tag, @attribute = tag, attribute
      super("attribute <#{attribute}> is missing in tag <#{tag}>")
    end
  end

  class UnknownTagError < InvalidRSSError
    attr_reader :tag, :uri
    def initialize(tag, uri)
      @tag, @uri = tag, uri
      super("tag <#{tag}> is unknown in namespace specified by uri <#{uri}>")
    end
  end

  class NotExpectedTagError < InvalidRSSError
    attr_reader :tag, :uri, :parent
    def initialize(tag, uri, parent)
      @tag, @uri, @parent = tag, uri, parent
      super("tag <{#{uri}}#{tag}> is not expected in tag <#{parent}>")
    end
  end
  # For backward compatibility :X
  NotExceptedTagError = NotExpectedTagError

  class NotAvailableValueError < InvalidRSSError
    attr_reader :tag, :value, :attribute
    def initialize(tag, value, attribute=nil)
      @tag, @value, @attribute = tag, value, attribute
      message = "value <#{value}> of "
      message << "attribute <#{attribute}> of " if attribute
      message << "tag <#{tag}> is not available."
      super(message)
    end
  end

  class UnknownConversionMethodError < Error
    attr_reader :to, :from
    def initialize(to, from)
      @to = to
      @from = from
      super("can't convert to #{to} from #{from}.")
    end
  end
  # for backward compatibility
  UnknownConvertMethod = UnknownConversionMethodError

  class ConversionError < Error
    attr_reader :string, :to, :from
    def initialize(string, to, from)
      @string = string
      @to = to
      @from = from
      super("can't convert #{@string} to #{to} from #{from}.")
    end
  end

  class NotSetError < Error
    attr_reader :name, :variables
    def initialize(name, variables)
      @name = name
      @variables = variables
      super("required variables of #{@name} are not set: #{@variables.join(', ')}")
    end
  end
  
  module BaseModel

    include Utils

    def install_have_child_element(tag_name, uri, occurs, name=nil)
      name ||= tag_name
      add_need_initialize_variable(name)
      install_model(tag_name, uri, occurs, name)

      attr_accessor name
      install_element(name) do |n, elem_name|
        <<-EOC
        if @#{n}
          "\#{@#{n}.to_s(need_convert, indent)}"
        else
          ''
        end
EOC
      end
    end
    alias_method(:install_have_attribute_element, :install_have_child_element)

    def install_have_children_element(tag_name, uri, occurs, name=nil, plural_name=nil)
      name ||= tag_name
      plural_name ||= "#{name}s"
      add_have_children_element(name, plural_name)
      add_plural_form(name, plural_name)
      install_model(tag_name, uri, occurs, plural_name)

      def_children_accessor(name, plural_name)
      install_element(name, "s") do |n, elem_name|
        <<-EOC
        rv = []
        @#{n}.each do |x|
          value = "\#{x.to_s(need_convert, indent)}"
          rv << value if /\\A\\s*\\z/ !~ value
        end
        rv.join("\n")
EOC
      end
    end

    def install_text_element(tag_name, uri, occurs, name=nil, type=nil, disp_name=nil)
      name ||= tag_name
      disp_name ||= name
      self::ELEMENTS << name
      add_need_initialize_variable(name)
      install_model(tag_name, uri, occurs, name)

      def_corresponded_attr_writer name, type, disp_name
      convert_attr_reader name
      install_element(name) do |n, elem_name|
        <<-EOC
        if @#{n}
          rv = "\#{indent}<#{elem_name}>"
          value = html_escape(@#{n})
          if need_convert
            rv << convert(value)
          else
            rv << value
          end
    	    rv << "</#{elem_name}>"
          rv
        else
          ''
        end
EOC
      end
    end

    def install_date_element(tag_name, uri, occurs, name=nil, type=nil, disp_name=nil)
      name ||= tag_name
      type ||= :w3cdtf
      disp_name ||= name
      self::ELEMENTS << name
      add_need_initialize_variable(name)
      install_model(tag_name, uri, occurs, name)

      # accessor
      convert_attr_reader name
      date_writer(name, type, disp_name)
      
      install_element(name) do |n, elem_name|
        <<-EOC
        if @#{n}
          rv = "\#{indent}<#{elem_name}>"
          value = html_escape(@#{n}.#{type})
          if need_convert
            rv << convert(value)
          else
            rv << value
          end
    	    rv << "</#{elem_name}>"
          rv
        else
          ''
        end
EOC
      end

    end

    private
    def install_element(name, postfix="")
      elem_name = name.sub('_', ':')
      method_name = "#{name}_element#{postfix}"
      add_to_element_method(method_name)
      module_eval(<<-EOC, *get_file_and_line_from_caller(2))
      def #{method_name}(need_convert=true, indent='')
        #{yield(name, elem_name)}
      end
      private :#{method_name}
EOC
    end

    def convert_attr_reader(*attrs)
      attrs.each do |attr|
        attr = attr.id2name if attr.kind_of?(Integer)
        module_eval(<<-EOC, *get_file_and_line_from_caller(2))
        def #{attr}
          if @converter
            @converter.convert(@#{attr})
          else
            @#{attr}
          end
        end
EOC
      end
    end

    def date_writer(name, type, disp_name=name)
      module_eval(<<-EOC, *get_file_and_line_from_caller(2))
      def #{name}=(new_value)
        if new_value.nil? or new_value.kind_of?(Time)
          @#{name} = new_value
        else
          if @do_validate
            begin
              @#{name} = Time.__send__('#{type}', new_value)
            rescue ArgumentError
              raise NotAvailableValueError.new('#{disp_name}', new_value)
            end
          else
            @#{name} = nil
            if /\\A\\s*\\z/ !~ new_value.to_s
              begin
                @#{name} = Time.parse(new_value)
              rescue ArgumentError
              end
            end
          end
        end

        # Is it need?
        if @#{name}
          class << @#{name}
            undef_method(:to_s)
            alias_method(:to_s, :#{type})
          end
        end

      end
EOC
    end

    def integer_writer(name, disp_name=name)
      module_eval(<<-EOC, *get_file_and_line_from_caller(2))
      def #{name}=(new_value)
        if new_value.nil?
          @#{name} = new_value
        else
          if @do_validate
            begin
              @#{name} = Integer(new_value)
            rescue ArgumentError
              raise NotAvailableValueError.new('#{disp_name}', new_value)
            end
          else
            @#{name} = new_value.to_i
          end
        end
      end
EOC
    end

    def positive_integer_writer(name, disp_name=name)
      module_eval(<<-EOC, *get_file_and_line_from_caller(2))
      def #{name}=(new_value)
        if new_value.nil?
          @#{name} = new_value
        else
          if @do_validate
            begin
              tmp = Integer(new_value)
              raise ArgumentError if tmp <= 0
              @#{name} = tmp
            rescue ArgumentError
              raise NotAvailableValueError.new('#{disp_name}', new_value)
            end
          else
            @#{name} = new_value.to_i
          end
        end
      end
EOC
    end

    def boolean_writer(name, disp_name=name)
      module_eval(<<-EOC, *get_file_and_line_from_caller(2))
      def #{name}=(new_value)
        if new_value.nil?
          @#{name} = new_value
        else
          if @do_validate and
              ![true, false, "true", "false"].include?(new_value)
            raise NotAvailableValueError.new('#{disp_name}', new_value)
          end
          if [true, false].include?(new_value)
            @#{name} = new_value
          else
            @#{name} = new_value == "true"
          end
        end
      end
EOC
    end

    def def_children_accessor(accessor_name, plural_name)
      module_eval(<<-EOC, *get_file_and_line_from_caller(2))
      def #{plural_name}
        @#{accessor_name}
      end

      def #{accessor_name}(*args)
        if args.empty?
          @#{accessor_name}.first
        else
          @#{accessor_name}[*args]
        end
      end

      def #{accessor_name}=(*args)
        warn("Warning:\#{caller.first.sub(/:in `.*'\z/, '')}: " \
             "Don't use `#{accessor_name} = XXX'/`set_#{accessor_name}(XXX)'. " \
             "Those APIs are not sense of Ruby. " \
             "Use `#{plural_name} << XXX' instead of them.")
        if args.size == 1
          @#{accessor_name}.push(args[0])
        else
          @#{accessor_name}.__send__("[]=", *args)
        end
      end
      alias_method(:set_#{accessor_name}, :#{accessor_name}=)
EOC
    end
  end

  class Element

    extend BaseModel
    include Utils

    INDENT = "  "
    
    MUST_CALL_VALIDATORS = {}
    MODELS = []
    GET_ATTRIBUTES = []
    HAVE_CHILDREN_ELEMENTS = []
    TO_ELEMENT_METHODS = []
    NEED_INITIALIZE_VARIABLES = []
    PLURAL_FORMS = {}
    
    class << self

      def must_call_validators
        MUST_CALL_VALIDATORS
      end
      def models
        MODELS
      end
      def get_attributes
        GET_ATTRIBUTES
      end
      def have_children_elements
        HAVE_CHILDREN_ELEMENTS
      end
      def to_element_methods
        TO_ELEMENT_METHODS
      end
      def need_initialize_variables
        NEED_INITIALIZE_VARIABLES
      end
      def plural_forms
        PLURAL_FORMS
      end

      
      def inherited(klass)
        klass.const_set("MUST_CALL_VALIDATORS", {})
        klass.const_set("MODELS", [])
        klass.const_set("GET_ATTRIBUTES", [])
        klass.const_set("HAVE_CHILDREN_ELEMENTS", [])
        klass.const_set("TO_ELEMENT_METHODS", [])
        klass.const_set("NEED_INITIALIZE_VARIABLES", [])
        klass.const_set("PLURAL_FORMS", {})

        klass.module_eval(<<-EOC)
        public
        
        @tag_name = name.split(/::/).last
        @tag_name[0,1] = @tag_name[0,1].downcase
        @have_content = false

        def self.must_call_validators
          super.merge(MUST_CALL_VALIDATORS)
        end
        def self.models
          MODELS + super
        end
        def self.get_attributes
          GET_ATTRIBUTES + super
        end
        def self.have_children_elements
          HAVE_CHILDREN_ELEMENTS + super
        end
        def self.to_element_methods
          TO_ELEMENT_METHODS + super
        end
        def self.need_initialize_variables
          NEED_INITIALIZE_VARIABLES + super
        end
        def self.plural_forms
          super.merge(PLURAL_FORMS)
        end

      
        def self.install_must_call_validator(prefix, uri)
          MUST_CALL_VALIDATORS[uri] = prefix
        end
        
        def self.install_model(tag, uri, occurs=nil, getter=nil)
          getter ||= tag
          if m = MODELS.find {|t, u, o, g| t == tag and u == uri}
            m[2] = occurs
          else
            MODELS << [tag, uri, occurs, getter]
          end
        end

        def self.install_get_attribute(name, uri, required=true,
                                       type=nil, disp_name=nil,
                                       element_name=nil)
          disp_name ||= name
          element_name ||= name
          def_corresponded_attr_writer name, type, disp_name
          convert_attr_reader name
          if type == :boolean and /^is/ =~ name
            alias_method "\#{$POSTMATCH}?", name
          end
          GET_ATTRIBUTES << [name, uri, required, element_name]
          add_need_initialize_variable(disp_name)
        end

        def self.def_corresponded_attr_writer(name, type=nil, disp_name=name)
          case type
          when :integer
            integer_writer name, disp_name
          when :positive_integer
            positive_integer_writer name, disp_name
          when :boolean
            boolean_writer name, disp_name
          when :w3cdtf, :rfc822, :rfc2822
            date_writer name, type, disp_name
          else
            attr_writer name
          end
        end

        def self.content_setup(type=nil)
          def_corresponded_attr_writer "content", type
          convert_attr_reader :content
          @have_content = true
        end

        def self.have_content?
          @have_content
        end

        def self.add_have_children_element(variable_name, plural_name)
          HAVE_CHILDREN_ELEMENTS << [variable_name, plural_name]
        end
        
        def self.add_to_element_method(method_name)
          TO_ELEMENT_METHODS << method_name
        end

        def self.add_need_initialize_variable(variable_name)
          NEED_INITIALIZE_VARIABLES << variable_name
        end
        
        def self.add_plural_form(singular, plural)
          PLURAL_FORMS[singular] = plural
        end
        
        EOC
      end

      def required_prefix
        nil
      end

      def required_uri
        ""
      end
      
      def install_ns(prefix, uri)
        if self::NSPOOL.has_key?(prefix)
          raise OverlappedPrefixError.new(prefix)
        end
        self::NSPOOL[prefix] = uri
      end

      def tag_name
        @tag_name
      end
    end

    attr_accessor :do_validate

    def initialize(do_validate=true, attrs={})
      @converter = nil
      @do_validate = do_validate
      initialize_variables(attrs)
    end

    def tag_name
      self.class.tag_name
    end

    def full_name
      tag_name
    end
    
    def converter=(converter)
      @converter = converter
      targets = children.dup
      self.class.have_children_elements.each do |variable_name, plural_name|
        targets.concat(__send__(plural_name))
      end
      targets.each do |target|
        target.converter = converter unless target.nil?
      end
    end

    def convert(value)
      if @converter
        @converter.convert(value)
      else
        value
      end
    end
    
    def validate(ignore_unknown_element=true)
      validate_attribute
      __validate(ignore_unknown_element)
    end
    
    def validate_for_stream(tags, ignore_unknown_element=true)
      validate_attribute
      __validate(ignore_unknown_element, tags, false)
    end

    def setup_maker(maker)
      target = maker_target(maker)
      unless target.nil?
        setup_maker_attributes(target)
        setup_maker_element(target)
        setup_maker_elements(target)
      end
    end

    def to_s(need_convert=true, indent='')
      if self.class.have_content?
        return "" unless @content
        rv = tag(indent) do |next_indent|
          h(@content)
        end
      else
        rv = tag(indent) do |next_indent|
          self.class.to_element_methods.collect do |method_name|
            __send__(method_name, false, next_indent)
          end
        end
      end
      rv = convert(rv) if need_convert
      rv
    end

    private
    def initialize_variables(attrs)
      normalized_attrs = {}
      attrs.each do |key, value|
        normalized_attrs[key.to_s] = value
      end
      self.class.need_initialize_variables.each do |variable_name|
        value = normalized_attrs[variable_name.to_s]
        if value
          __send__("#{variable_name}=", value)
        else
          instance_eval("@#{variable_name} = nil")
        end
      end
      initialize_have_children_elements
      @content = "" if self.class.have_content?
    end

    def initialize_have_children_elements
      self.class.have_children_elements.each do |variable_name, plural_name|
        instance_eval("@#{variable_name} = []")
      end
    end

    def tag(indent, additional_attrs={}, &block)
      next_indent = indent + INDENT

      attrs = collect_attrs
      return "" if attrs.nil?

      attrs.update(additional_attrs)
      start_tag = make_start_tag(indent, next_indent, attrs)

      if block
        content = block.call(next_indent)
      else
        content = []
      end

      if content.is_a?(String)
        content = [content]
        start_tag << ">"
        end_tag = "</#{full_name}>"
      else
        content = content.reject{|x| x.empty?}
        if content.empty?
          end_tag = "/>"
        else
          start_tag << ">\n"
          end_tag = "\n#{indent}</#{full_name}>"
        end
      end
      
      start_tag + content.join("\n") + end_tag
    end

    def make_start_tag(indent, next_indent, attrs)
      start_tag = ["#{indent}<#{full_name}"]
      unless attrs.empty?
        start_tag << attrs.collect do |key, value|
          %Q[#{h key}="#{h value}"]
        end.join("\n#{next_indent}")
      end
      start_tag.join(" ")
    end

    def collect_attrs
      attrs = {}
      _attrs.each do |name, required, alias_name|
        value = __send__(alias_name || name)
        return nil if required and value.nil?
        next if value.nil?
        return nil if attrs.has_key?(name)
        attrs[name] = value
      end
      attrs
    end
    
    def tag_name_with_prefix(prefix)
      "#{prefix}:#{tag_name}"
    end

    # For backward compatibility
    def calc_indent
      ''
    end

    def maker_target(maker)
      nil
    end
    
    def setup_maker_attributes(target)
    end
    
    def setup_maker_element(target)
      self.class.need_initialize_variables.each do |var|
        value = __send__(var)
        if value.respond_to?("setup_maker") and
            !not_need_to_call_setup_maker_variables.include?(var)
          value.setup_maker(target)
        else
          setter = "#{var}="
          if target.respond_to?(setter)
            target.__send__(setter, value)
          end
        end
      end
    end

    def not_need_to_call_setup_maker_variables
      []
    end
    
    def setup_maker_elements(parent)
      self.class.have_children_elements.each do |name, plural_name|
        if parent.respond_to?(plural_name)
          target = parent.__send__(plural_name)
          __send__(plural_name).each do |elem|
            elem.setup_maker(target)
          end
        end
      end
    end

    def set_next_element(tag_name, next_element)
      klass = next_element.class
      prefix = ""
      prefix << "#{klass.required_prefix}_" if klass.required_prefix
      key = "#{prefix}#{tag_name}"
      if self.class.plural_forms.has_key?(key)
        ary = __send__("#{self.class.plural_forms[key]}")
        ary << next_element
      else
        __send__("#{prefix}#{tag_name}=", next_element)
      end
    end

    def children
      rv = []
      self.class.models.each do |name, uri, occurs, getter|
        value = __send__(getter)
        next if value.nil?
        value = [value] unless value.is_a?(Array)
        value.each do |v|
          rv << v if v.is_a?(Element)
        end
      end
      rv
    end

    def _tags
      rv = []
      self.class.models.each do |name, uri, occurs, getter|
        value = __send__(getter)
        next if value.nil?
        if value.is_a?(Array)
          rv.concat([[uri, name]] * value.size)
        else
          rv << [uri, name]
        end
      end
      rv
    end

    def _attrs
      self.class.get_attributes.collect do |name, uri, required, element_name|
        [element_name, required, name]
      end
    end

    def __validate(ignore_unknown_element, tags=_tags, recursive=true)
      if recursive
        children.compact.each do |child|
          child.validate
        end
      end
      must_call_validators = self.class.must_call_validators
      tags = tag_filter(tags.dup)
      p tags if DEBUG
      must_call_validators.each do |uri, prefix|
        _validate(ignore_unknown_element, tags[uri], uri)
        meth = "#{prefix}_validate"
        if respond_to?(meth, true)
          __send__(meth, ignore_unknown_element, tags[uri], uri)
        end
      end
    end

    def validate_attribute
      _attrs.each do |a_name, required, alias_name|
        if required and __send__(alias_name || a_name).nil?
          raise MissingAttributeError.new(tag_name, a_name)
        end
      end
    end

    def _validate(ignore_unknown_element, tags, uri, models=self.class.models)
      count = 1
      do_redo = false
      not_shift = false
      tag = nil
      models = models.find_all {|model| model[1] == uri}
      element_names = models.collect {|model| model[0]}
      if tags
        tags_size = tags.size
        tags = tags.sort_by {|x| element_names.index(x) || tags_size}
      end

      models.each_with_index do |model, i|
        name, model_uri, occurs, getter = model

        if DEBUG
          p "before" 
          p tags
          p model
        end

        if not_shift
          not_shift = false
        elsif tags
          tag = tags.shift
        end

        if DEBUG
          p "mid"
          p count
        end

        case occurs
        when '?'
          if count > 2
            raise TooMuchTagError.new(name, tag_name)
          else
            if name == tag
              do_redo = true
            else
              not_shift = true
            end
          end
        when '*'
          if name == tag
            do_redo = true
          else
            not_shift = true
          end
        when '+'
          if name == tag
            do_redo = true
          else
            if count > 1
              not_shift = true
            else
              raise MissingTagError.new(name, tag_name)
            end
          end
        else
          if name == tag
            if models[i+1] and models[i+1][0] != name and
                tags and tags.first == name
              raise TooMuchTagError.new(name, tag_name)
            end
          else
            raise MissingTagError.new(name, tag_name)
          end
        end

        if DEBUG
          p "after"
          p not_shift
          p do_redo
          p tag
        end

        if do_redo
          do_redo = false
          count += 1
          redo
        else
          count = 1
        end

      end

      if !ignore_unknown_element and !tags.nil? and !tags.empty?
        raise NotExpectedTagError.new(tags.first, uri, tag_name)
      end

    end

    def tag_filter(tags)
      rv = {}
      tags.each do |tag|
        rv[tag[0]] = [] unless rv.has_key?(tag[0])
        rv[tag[0]].push(tag[1])
      end
      rv
    end

  end

  module RootElementMixin

    include XMLStyleSheetMixin
    
    attr_reader :output_encoding

    def initialize(rss_version, version=nil, encoding=nil, standalone=nil)
      super()
      @rss_version = rss_version
      @version = version || '1.0'
      @encoding = encoding
      @standalone = standalone
      @output_encoding = nil
    end

    def output_encoding=(enc)
      @output_encoding = enc
      self.converter = Converter.new(@output_encoding, @encoding)
    end

    def setup_maker(maker)
      maker.version = version
      maker.encoding = encoding
      maker.standalone = standalone

      xml_stylesheets.each do |xss|
        xss.setup_maker(maker)
      end

      setup_maker_elements(maker)
    end

    def to_xml(version=nil, &block)
      if version.nil? or version == @rss_version
        to_s
      else
        RSS::Maker.make(version) do |maker|
          setup_maker(maker)
          block.call(maker) if block
        end.to_s
      end
    end

    private
    def tag(indent, attrs={}, &block)
      rv = xmldecl + xml_stylesheet_pi
      rv << super(indent, ns_declarations.merge(attrs), &block)
      rv
    end

    def xmldecl
      rv = %Q[<?xml version="#{@version}"]
      if @output_encoding or @encoding
        rv << %Q[ encoding="#{@output_encoding or @encoding}"]
      end
      rv << %Q[ standalone="yes"] if @standalone
      rv << "?>\n"
      rv
    end
    
    def ns_declarations
      decls = {}
      self.class::NSPOOL.collect do |prefix, uri|
        prefix = ":#{prefix}" unless prefix.empty?
        decls["xmlns#{prefix}"] = uri
      end
      decls
    end
    
    def setup_maker_elements(maker)
      channel.setup_maker(maker) if channel
      image.setup_maker(maker) if image
      textinput.setup_maker(maker) if textinput
      super(maker)
    end
  end

end
