#
# A wrapper for IMessageSink
#

module Gallio
  module Wrappers
    class MessageSink
      attr_reader :inner
        
      def initialize(inner)
        @inner = inner
      end
      
      def publish(message)
        @inner.Publish(message)
      end
    end
  end
end
