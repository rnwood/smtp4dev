require File.join(File.dirname(__FILE__), "/load-diff-lcs")
require 'pp'

module Spec
  module Expectations
    module Differs
      unless defined?(Default)
        class Default
          def initialize(options)
            @options = options
          end

          # This is snagged from diff/lcs/ldiff.rb (which is a commandline tool)
          def diff_as_string(data_new, data_old)
            data_old = data_old.split(/\n/).map! { |e| e.chomp }
            data_new = data_new.split(/\n/).map! { |e| e.chomp }
            output = ""
            diffs = Diff::LCS.diff(data_old, data_new)
            return output if diffs.empty?
            oldhunk = hunk = nil  
            file_length_difference = 0
            diffs.each do |piece|
              begin
                hunk = Diff::LCS::Hunk.new(data_old, data_new, piece, context_lines,
                                           file_length_difference)
                file_length_difference = hunk.file_length_difference      
                next unless oldhunk      
                # Hunks may overlap, which is why we need to be careful when our
                # diff includes lines of context. Otherwise, we might print
                # redundant lines.
                if (context_lines > 0) and hunk.overlaps?(oldhunk)
                  hunk.unshift(oldhunk)
                else
                  output << oldhunk.diff(format)
                end
              ensure
                oldhunk = hunk
                output << "\n"
              end
            end  
            #Handle the last remaining hunk
            output << oldhunk.diff(format) << "\n"
          end  

          def diff_as_object(target,expected)
            diff_as_string(PP.pp(target,""), PP.pp(expected,""))
          end

          protected
          def format
            @options.diff_format
          end

          def context_lines
            @options.context_lines
          end
        end

      end
    end
  end
end
