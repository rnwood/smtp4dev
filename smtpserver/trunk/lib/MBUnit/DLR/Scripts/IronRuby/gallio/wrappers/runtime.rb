#
# A wrapper for the runtime.
#

module Gallio
  module Wrappers
    class Runtime
      def self.Resolve(service_type_module)
        ::Gallio::Runtime::RuntimeAccessor.ServiceLocator.Resolve(service_type_module.to_clr_type)
      end
    end
  end
end
