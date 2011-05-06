#
# A wrapper for IProgressMonitor
#

module Gallio
  module Wrappers
    class ProgressMonitor
      attr_reader :inner
    
      def initialize(inner)
        @inner = inner
      end
    
      def begin_task(task_name, total_work_units)
        @inner.BeginTask(task_name, total_work_units)
      end
      
      def done()
        @inner.Done()
      end
      
      def is_canceled?
        @inner.IsCanceled
      end
    
      def set_status(message)
        @inner.SetStatus(message)
      end
      
      def worked(work_units)
        @inner.Worked(work_units)
      end
      
      def create_sub_progress_monitor(parent_work_units)
        new(@inner.CreateSubProgressMonitor(parent_work_units))
      end
    end
  end
end
