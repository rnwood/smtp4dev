require "forwardable"
require "open-uri"

require "rss/rss"

module RSS

  class NotWellFormedError < Error
    attr_reader :line, :element

    # Create a new NotWellFormedError for an error at +line+
    # in +element+.  If a block is given the return value of
    # the block ends up in the error message.
    def initialize(line=nil, element=nil)
      message = "This is not well formed XML"
      if element or line
        message << "\nerror occurred"
        message << " in #{element}" if element
        message << " at about #{line} line" if line
      end
      message << "\n#{yield}" if block_given?
      super(message)
    end
  end

  class XMLParserNotFound < Error
    def initialize
      super("available XML parser was not found in " <<
            "#{AVAILABLE_PARSER_LIBRARIES.inspect}.")
    end
  end

  class NotValidXMLParser < Error
    def initialize(parser)
      super("#{parser} is not an available XML parser. " <<
            "Available XML parser"<<
            (AVAILABLE_PARSERS.size > 1 ? "s are ": " is ") <<
            "#{AVAILABLE_PARSERS.inspect}.")
    end
  end

  class NSError < InvalidRSSError
    attr_reader :tag, :prefix, :uri
    def initialize(tag, prefix, require_uri)
      @tag, @prefix, @uri = tag, prefix, require_uri
      super("prefix <#{prefix}> doesn't associate uri " <<
            "<#{require_uri}> in tag <#{tag}>")
    end
  end

  class Parser

    extend Forwardable

    class << self

      @@default_parser = nil

      def default_parser
        @@default_parser || AVAILABLE_PARSERS.first
      end

      # Set @@default_parser to new_value if it is one of the
      # available parsers. Else raise NotValidXMLParser error.
      def default_parser=(new_value)
        if AVAILABLE_PARSERS.include?(new_value)
          @@default_parser = new_value
        else
          raise NotValidXMLParser.new(new_value)
        end
      end

      def parse(rss, do_validate=true, ignore_unknown_element=true,
                parser_class=default_parser)
        parser = new(rss, parser_class)
        parser.do_validate = do_validate
        parser.ignore_unknown_element = ignore_unknown_element
        parser.parse
      end
    end

    def_delegators(:@parser, :parse, :rss,
                   :ignore_unknown_element,
                   :ignore_unknown_element=, :do_validate,
                   :do_validate=)

    def initialize(rss, parser_class=self.class.default_parser)
      @parser = parser_class.new(normalize_rss(rss))
    end

    private

    # Try to get the XML associated with +rss+.
    # Return +rss+ if it already looks like XML, or treat it as a URI,
    # or a file to get the XML,
    def normalize_rss(rss)
      return rss if maybe_xml?(rss)

      uri = to_uri(rss)
      
      if uri.respond_to?(:read)
        uri.read
      elsif !rss.tainted? and File.readable?(rss)
        File.open(rss) {|f| f.read}
      else
        rss
      end
    end

    # maybe_xml? tests if source is a string that looks like XML.
    def maybe_xml?(source)
      source.is_a?(String) and /</ =~ source
    end

    # Attempt to convert rss to a URI, but just return it if 
    # there's a ::URI::Error
    def to_uri(rss)
      return rss if rss.is_a?(::URI::Generic)

      begin
        URI(rss)
      rescue ::URI::Error
        rss
      end
    end
  end

  class BaseParser

    class << self
      def raise_for_undefined_entity?
        listener.raise_for_undefined_entity?
      end
    end
    
    def initialize(rss)
      @listener = self.class.listener.new
      @rss = rss
    end

    def rss
      @listener.rss
    end

    def ignore_unknown_element
      @listener.ignore_unknown_element
    end

    def ignore_unknown_element=(new_value)
      @listener.ignore_unknown_element = new_value
    end

    def do_validate
      @listener.do_validate
    end

    def do_validate=(new_value)
      @listener.do_validate = new_value
    end

    def parse
      if @listener.rss.nil?
        _parse
      end
      @listener.rss
    end

  end

  class BaseListener

    extend Utils

    class << self

      @@setters = {}
      @@registered_uris = {}
      @@class_names = {}

      # return the setter for the uri, tag_name pair, or nil.
      def setter(uri, tag_name)
        begin
          @@setters[uri][tag_name]
        rescue NameError
          nil
        end
      end


      # return the tag_names for setters associated with uri
      def available_tags(uri)
        begin
          @@setters[uri].keys
        rescue NameError
          []
        end
      end
      
      # register uri against this name.
      def register_uri(uri, name)
        @@registered_uris[name] ||= {}
        @@registered_uris[name][uri] = nil
      end
      
      # test if this uri is registered against this name
      def uri_registered?(uri, name)
        @@registered_uris[name].has_key?(uri)
      end

      # record class_name for the supplied uri and tag_name
      def install_class_name(uri, tag_name, class_name)
        @@class_names[uri] ||= {}
        @@class_names[uri][tag_name] = class_name
      end

      # retrieve class_name for the supplied uri and tag_name
      # If it doesn't exist, capitalize the tag_name
      def class_name(uri, tag_name)
        begin
          @@class_names[uri][tag_name]
        rescue NameError
          tag_name[0,1].upcase + tag_name[1..-1]
        end
      end

      def install_get_text_element(uri, name, setter)
        install_setter(uri, name, setter)
        def_get_text_element(uri, name, *get_file_and_line_from_caller(1))
      end
      
      def raise_for_undefined_entity?
        true
      end
    
      private
      # set the setter for the uri, tag_name pair
      def install_setter(uri, tag_name, setter)
        @@setters[uri] ||= {}
        @@setters[uri][tag_name] = setter
      end

      def def_get_text_element(uri, name, file, line)
        register_uri(uri, name)
        unless private_instance_methods(false).include?("start_#{name}")
          module_eval(<<-EOT, file, line)
          def start_#{name}(name, prefix, attrs, ns)
            uri = _ns(ns, prefix)
            if self.class.uri_registered?(uri, #{name.inspect})
              start_get_text_element(name, prefix, ns, uri)
            else
              start_else_element(name, prefix, attrs, ns)
            end
          end
          EOT
          __send__("private", "start_#{name}")
        end
      end

    end

  end

  module ListenerMixin

    attr_reader :rss

    attr_accessor :ignore_unknown_element
    attr_accessor :do_validate

    def initialize
      @rss = nil
      @ignore_unknown_element = true
      @do_validate = true
      @ns_stack = [{}]
      @tag_stack = [[]]
      @text_stack = ['']
      @proc_stack = []
      @last_element = nil
      @version = @encoding = @standalone = nil
      @xml_stylesheets = []
    end
    
    # set instance vars for version, encoding, standalone
    def xmldecl(version, encoding, standalone)
      @version, @encoding, @standalone = version, encoding, standalone
    end

    def instruction(name, content)
      if name == "xml-stylesheet"
        params = parse_pi_content(content)
        if params.has_key?("href")
          @xml_stylesheets << XMLStyleSheet.new(*params)
        end
      end
    end

    def tag_start(name, attributes)
      @text_stack.push('')

      ns = @ns_stack.last.dup
      attrs = {}
      attributes.each do |n, v|
        if /\Axmlns(?:\z|:)/ =~ n
          ns[$POSTMATCH] = v
        else
          attrs[n] = v
        end
      end
      @ns_stack.push(ns)

      prefix, local = split_name(name)
      @tag_stack.last.push([_ns(ns, prefix), local])
      @tag_stack.push([])
      if respond_to?("start_#{local}", true)
        __send__("start_#{local}", local, prefix, attrs, ns.dup)
      else
        start_else_element(local, prefix, attrs, ns.dup)
      end
    end

    def tag_end(name)
      if DEBUG
        p "end tag #{name}"
        p @tag_stack
      end
      text = @text_stack.pop
      tags = @tag_stack.pop
      pr = @proc_stack.pop
      pr.call(text, tags) unless pr.nil?
      @ns_stack.pop
    end

    def text(data)
      @text_stack.last << data
    end

    private
    def _ns(ns, prefix)
      ns.fetch(prefix, "")
    end

    CONTENT_PATTERN = /\s*([^=]+)=(["'])([^\2]+?)\2/
    # Extract the first name="value" pair from content.
    # Works with single quotes according to the constant
    # CONTENT_PATTERN. Return a Hash.
    def parse_pi_content(content)
      params = {}
      content.scan(CONTENT_PATTERN) do |name, quote, value|
        params[name] = value
      end
      params
    end

    def start_else_element(local, prefix, attrs, ns)
      class_name = self.class.class_name(_ns(ns, prefix), local)
      current_class = @last_element.class
      if current_class.constants.include?(class_name)
        next_class = current_class.const_get(class_name)
        start_have_something_element(local, prefix, attrs, ns, next_class)
      else
        if !@do_validate or @ignore_unknown_element
          @proc_stack.push(nil)
        else
          parent = "ROOT ELEMENT???"
          if current_class.tag_name
            parent = current_class.tag_name
          end
          raise NotExpectedTagError.new(local, _ns(ns, prefix), parent)
        end
      end
    end

    NAMESPLIT = /^(?:([\w:][-\w\d.]*):)?([\w:][-\w\d.]*)/
    def split_name(name)
      name =~ NAMESPLIT
      [$1 || '', $2]
    end

    def check_ns(tag_name, prefix, ns, require_uri)
      if @do_validate
        if _ns(ns, prefix) == require_uri
          #ns.delete(prefix)
        else
          raise NSError.new(tag_name, prefix, require_uri)
        end
      end
    end

    def start_get_text_element(tag_name, prefix, ns, required_uri)
      @proc_stack.push Proc.new {|text, tags|
        setter = self.class.setter(required_uri, tag_name)
        if @last_element.respond_to?(setter)
          @last_element.__send__(setter, text.to_s)
        else
          if @do_validate and !@ignore_unknown_element
            raise NotExpectedTagError.new(tag_name, _ns(ns, prefix),
                                          @last_element.tag_name)
          end
        end
      }
    end

    def start_have_something_element(tag_name, prefix, attrs, ns, klass)

      check_ns(tag_name, prefix, ns, klass.required_uri)

      attributes = {}
      klass.get_attributes.each do |a_name, a_uri, required, element_name|

        if a_uri.is_a?(String) or !a_uri.respond_to?(:include?)
          a_uri = [a_uri]
        end
        unless a_uri == [""]
          for prefix, uri in ns
            if a_uri.include?(uri)
              val = attrs["#{prefix}:#{a_name}"]
              break if val
            end
          end
        end
        if val.nil? and a_uri.include?("")
          val = attrs[a_name]
        end

        if @do_validate and required and val.nil?
          unless a_uri.include?("")
            for prefix, uri in ns
              if a_uri.include?(uri)
                a_name = "#{prefix}:#{a_name}"
              end
            end
          end
          raise MissingAttributeError.new(tag_name, a_name)
        end

        attributes[a_name] = val
      end

      previous = @last_element
      next_element = klass.new(@do_validate, attributes)
      previous.instance_eval {set_next_element(tag_name, next_element)}
      @last_element = next_element
      @proc_stack.push Proc.new { |text, tags|
        p(@last_element.class) if DEBUG
        @last_element.content = text if klass.have_content?
        if @do_validate
          @last_element.validate_for_stream(tags, @ignore_unknown_element)
        end
        @last_element = previous
      }
    end

  end

  unless const_defined? :AVAILABLE_PARSER_LIBRARIES
    AVAILABLE_PARSER_LIBRARIES = [
      ["rss/xmlparser", :XMLParserParser],
      ["rss/xmlscanner", :XMLScanParser],
      ["rss/rexmlparser", :REXMLParser],
    ]
  end

  AVAILABLE_PARSERS = []

  AVAILABLE_PARSER_LIBRARIES.each do |lib, parser|
    begin
      require lib
      AVAILABLE_PARSERS.push(const_get(parser))
    rescue LoadError
    end
  end

  if AVAILABLE_PARSERS.empty?
    raise XMLParserNotFound
  end
end
