#
# A wrapper for ILogger.
#

module Gallio
  module Wrappers
    class Logger
      attr_reader :inner
    
      def initialize(inner)
        @inner = inner
      end

      def error(message)
        log(::Gallio::Runtime::Logging::LogSeverity.Error, message)
      end

      def warning(message)
        log(::Gallio::Runtime::Logging::LogSeverity.Warning, message)
      end

      def important(message)
        log(::Gallio::Runtime::Logging::LogSeverity.Important, message)
      end

      def info(message)
        log(::Gallio::Runtime::Logging::LogSeverity.Info, message)
      end
      
      def debug(message)
        log(::Gallio::Runtime::Logging::LogSeverity.Debug, message)
      end
      
    private
      
      def log(severity, message)      
        @inner.Log(severity, message)
      end
    end
  end
end
