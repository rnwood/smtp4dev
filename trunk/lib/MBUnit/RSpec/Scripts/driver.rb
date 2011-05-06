# driver.rb
#
# Runs RSpec tests.
#
# This script receives inputs from Gallio in the form of a dictionary called ScriptParameters.
# See the class comments on DLRTestDriver for more information about these parameters.
#

require 'gallio'
require 'spec'
require 'spec/runner/formatter/base_formatter'
require 'shellwords'

module Gallio
  module RSpecAdapter
  
    class Driver
      def initialize(parameters)
        @verb = parameters['Verb']
        @test_package = parameters['TestPackage']
        @test_exploration_options = parameters['TestExplorationOptions']
        @text_execution_options = parameters['TestExecutionOptions']
        @message_sink = ::Gallio::Wrappers::MessageSink.new(parameters['MessageSink'])
        @progress_monitor = ::Gallio::Wrappers::ProgressMonitor.new(parameters['ProgressMonitor'])
        @logger = ::Gallio::Wrappers::Logger.new(parameters['Logger'])
      end
      
      def go
        @logger.debug('RSpec adapter started.')
      
        case @verb
          when 'Explore'
            explore
          
          when 'Run'
            run
        end
            
      ensure      
        @logger.debug('RSpec adapter finished.')        
      end
      
    private
      
      def explore
        explore_or_run(true)
      end
      
      def run
        explore_or_run(false)
      end
      
      def explore_or_run(exploring)
        @progress_monitor.begin_task(exploring ? "Exploring RSpec tests." : "Running RSpec tests.", 1)
          
        options = create_options(exploring)
        options.formatters << Formatter.new(options, @message_sink, @progress_monitor)
        
        ::Spec::Runner.use options
        ::Spec::Runner.run
        
        @progress_monitor.done
      end
      
      def create_options(exploring)
        options_str = @test_package.Properties.GetValue('RSpecOptions') || ''
        @logger.debug("RSpec options: '#{options_str}'")
        
        options_array = ::Shellwords::shellwords(options_str)
        options_array << '--dry-run' if exploring
        options_array << '--format' << 'silent'
        @test_package.Files.each do |file|
          options_array << file.FullName
          dirName = file.Directory.FullName
          $: << dirName if $:.index(dirName) == nil
        end
        
        workaround_option_parser_use_of_argument_zero        
        parser = ::Spec::Runner::OptionParser.new($stderr, $stdout)
        parser.parse(options_array)
        
        parser.options
      end
      
      # HACK: Work around some bad code in OptionParser.spec_command? where it
      #       tries to read $0.  Unfortunately $0 is nil here.
      def workaround_option_parser_use_of_argument_zero
        $0 = ''  
      end
      
      #
      # The Formatter listens for events from the RSpec test runner and generates
      # messages for Gallio.  The implementation is a little bit complicated because
      # we have to rederive the file list and nested example group structure based
      # on flattened events.
      #
      # In some places we call ToString() on an existing string or on some other
      # object that is implicitly convertible to strings.  This is a workaround for
      # an error 'incompatible character encodings: utf-8 (KCODE) and Windows-1252'
      # that can appear when mixing literal strings with computed values.
      #
      class Formatter < ::Spec::Runner::Formatter::BaseFormatter
        def initialize(options, message_sink, progress_monitor)
          @options = options
          @message_sink = message_sink
          @progress_monitor = progress_monitor
          
          test_context_tracker = ::Gallio::Wrappers::Runtime.Resolve(::Gallio::Model::Contexts::ITestContextTracker)
          @test_context_manager = ::Gallio::Model::Contexts::ObservableTestContextManager.new(test_context_tracker, message_sink.inner)
          @test_stack = []
          @test_context_stack = []
          @work_per_example = 1
          @example_group_depth = 0
          @current_file_path = nil
        end
        
        def start(example_count)
          #puts "start"
          @work_per_example = 1.0 / example_count if example_count > 1
          
          # Push the root onto the stack.
          internal_root_started
        end

        def example_group_started(example_group_proxy)
          #puts "example_group_started"
          
          # Pop all deeper example groups and siblings off the stack.
          while @example_group_depth >= example_group_proxy.nested_descriptions.length
            internal_example_group_finished
          end

          # Check whether we are working on a new file.
          if @example_group_depth == 0
            path, line = parse_location(example_group_proxy.location)
            if path != @current_file_path
              internal_file_finished if @current_file_path != nil
              @current_file_path = path
              internal_file_started(path) if path != nil
            end
          end

          # Push this group onto the stack.
          internal_example_group_started(example_group_proxy)
        end
        
        def example_started(example_proxy)
          #puts "example_started"
          
          internal_example_started(example_proxy)
        end

        def example_passed(example_proxy)
          #puts "example_passed"
          
          internal_example_finished(::Gallio::Model::TestOutcome.Passed) do |example_test_context|
          end
        end

        def example_failed(example_proxy, counter, failure)
          #puts "example_failed"
          
          internal_example_finished(::Gallio::Model::TestOutcome.Failed) do |example_test_context|
            example_test_context.LogWriter.Failures.BeginSection(failure.header)
            example_test_context.LogWriter.Failures.Write(failure.exception.message) unless failure.exception.nil?
            example_test_context.LogWriter.Failures.Write(format_backtrace(failure.exception.backtrace)) unless failure.exception.nil?
            example_test_context.LogWriter.Failures.End()
          end
        end
        
        def example_pending(example_proxy, message, deprecated_pending_location=nil)
          #puts "example_pending"
          
          internal_example_finished(::Gallio::Model::TestOutcome.Pending) do |example_test_context|
            example_test_context.AddMetadata(::Gallio::Model::MetadataKeys.PendingReason, message)
            example_test_context.LogWriter.Warnings.BeginSection("Pending")
            example_test_context.LogWriter.Warnings.Write(message)
            example_test_context.LogWriter.Warnings.End()
          end
        end
        
        def start_dump
          #puts "start_dump"
          
          # Pop all example groups off the stack.
          while @example_group_depth > 0 do
            internal_example_group_finished
          end

          # Pop off the current file if there is one
          if @current_file_path != nil
            internal_file_finished
            @current_file_path = nil
          end

          # Pop the root off the stack.
          if @test_stack.length > 0
            internal_root_finished
          end
        end

        def close
          #puts "close"
        end
        
      private
      
        def internal_root_started
          root_test = create_root_test
          @test_stack.push(root_test)
          
          notify_test_discovered(root_test)
        
          return if dry_run?
          
          root_test_context = start_test_step(root_test)
          @test_context_stack.push(root_test_context)
        end
        
        def internal_root_finished
          @test_stack.pop()
          
          return if dry_run?
          
          root_test_context = @test_context_stack.pop()
          finish_test_step(root_test_context)
        end
      
        def internal_file_started(file_path)
          @progress_monitor.set_status("Running file: #{file_path}.")
          
          file_name = ::System::IO::Path.GetFileNameWithoutExtension(file_path)
          file_test = create_test(file_name)
          file_test.Kind = "RSpec File"
          file_test.Metadata.Add(::Gallio::Model::MetadataKeys.File, file_path)
          @test_stack.last.AddChild(file_test)
          @test_stack.push(file_test)
          
          notify_test_discovered(file_test)
          
          return if dry_run?
          
          file_test_context = start_test_step(file_test)
          @test_context_stack.push(file_test_context)          
        end
        
        def internal_file_finished
          @test_stack.pop()
          
          return if dry_run?
          
          file_test_context = @test_context_stack.pop()
          finish_test_step(file_test_context)
        end
      
        def internal_example_group_started(example_group_proxy)
          description = example_group_proxy.nested_descriptions.last.ToString()
          @progress_monitor.set_status("Running example group: #{description}.")
          
          example_group_test = create_test(description)
          example_group_test.Kind = "RSpec Example Group"
          @test_stack.last.AddChild(example_group_test)
          @test_stack.push(example_group_test)          
          @example_group_depth += 1
          
          notify_test_discovered(example_group_test, example_group_proxy.location)
          
          return if dry_run?
          
          example_group_test_context = start_test_step(example_group_test)
          @test_context_stack.push(example_group_test_context)
        end
      
        def internal_example_group_finished
          @example_group_depth -= 1
          @test_stack.pop()
          
          return if dry_run?
          
          example_group_test_context = @test_context_stack.pop()
          finish_test_step(example_group_test_context)
        end
        
        def internal_example_started(example_proxy)
          description = example_proxy.description.ToString()
          @progress_monitor.set_status("Running example: #{description}.")
          
          example_test = create_test(description)
          example_test.Kind = "RSpec Example"
          example_test.IsTestCase = true
          @test_stack.last.AddChild(example_test)
          @test_stack.push(example_test)
          
          notify_test_discovered(example_test, example_proxy.location)
          
          return if dry_run?
          
          example_test_context = start_test_step(example_test)
          @test_context_stack.push(example_test_context)
        end
        
        def internal_example_finished(outcome, &blk)
          @progress_monitor.worked(@work_per_example)
          @test_stack.pop()
          
          return if dry_run?
          
          example_test_context = @test_context_stack.pop()
          set_outcome(example_test_context, outcome)
          blk.call(example_test_context)
          finish_test_step(example_test_context)
        end
        
        def set_outcome(test_context, outcome)
          test_context.SetInterimOutcome(outcome)
        end
        
        def start_test_step(test)
          if @test_context_stack.length == 0
            test_step = create_test_step(test)
            test_context = @test_context_manager.StartStep(test_step)
          else
            test_step = create_test_step(test, @test_context_stack.last.TestStep)
            test_context = @test_context_stack.last.StartChildStep(test_step)
          end
          
          test_context
        end
        
        def finish_test_step(test_context)
          outcome = test_context.Outcome
          test_context.FinishStep(outcome, nil)
          
          parent_test_context = test_context.Parent
          if parent_test_context != nil
            if outcome.Status != ::Gallio::Model::TestStatus.Inconclusive
              parent_outcome = parent_test_context.Outcome.CombineWith(outcome).Generalize()
              parent_test_context.SetInterimOutcome(parent_outcome)
            end
          end
        end
        
        def format_backtrace(backtrace)
          return "" if backtrace.nil?
          backtrace.map { |line| backtrace_line(line) }.join("\n")
        end

        def backtrace_line(line)
          line.sub(/\A([^:]+:\d+)$/, '\\1:')
        end
      
        def dry_run?
          @options.dry_run?
        end
        
        def create_root_test
          ::Gallio::Model::Tree::RootTest.new
        end
      
        def create_test(name)
          ::Gallio::Model::Tree::Test.new(name, nil)
        end
        
        def create_test_step(test, parent_test_step = nil)
          ::Gallio::Model::Tree::TestStep.new(test, parent_test_step)
        end
        
        def notify_test_discovered(test, location = nil)
          message = ::Gallio::Model::Messages::Exploration::TestDiscoveredMessage.new
          message.ParentTestId = test.Parent.Id if test.Parent
          message.Test = ::Gallio::Model::Schema::TestData.new(test)
          
          path, line = parse_location(location)
          if path != nil
            message.Test.CodeLocation = ::Gallio::Common::Reflection::CodeLocation.new(path, line, 0)
          end
        
          @message_sink.publish(message)
        end
        
        def parse_location(location)
          if (location != nil)
            matches = /^(.*):([0-9]+)$/.match(location)
            if matches != nil
              [matches[1].ToString(), matches[2].to_i]
            else
              [location.ToString(), 0]
            end
          else
            [nil, 0]
          end
        end
      end
    end
  end
end

# Run.

::Gallio::RSpecAdapter::Driver.new(ScriptParameters).go
