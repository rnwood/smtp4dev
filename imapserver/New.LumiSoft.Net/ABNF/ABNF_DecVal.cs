using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.ABNF
{
    /// <summary>
    /// This class represent ABNF "dec-val". Defined in RFC 5234 4.
    /// </summary>
    public class ABNF_DecVal : ABNF_Element
    {
        #region enum ValueType

        private enum ValueType
        {
            Single = 0,
            Concated = 1,
            Range  = 2,
        }

        #endregion

        /// <summary>
        /// Default 'range' value constructor.
        /// </summary>
        /// <param name="start">Range start value.</param>
        /// <param name="end">Range end value.</param>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public ABNF_DecVal(int start,int end)
        {
            if(start < 0){
                throw new ArgumentException("Argument 'start' value must be >= 0.");
            }
            if(end < 0){
                throw new ArgumentException("Argument 'end' value must be >= 0.");
            }

            // TODO:
        }

        /// <summary>
        /// Default 'concated' value constructor.
        /// </summary>
        /// <param name="values">Concated values.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>values</b> is null reference value.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public ABNF_DecVal(int[] values)
        {
            if(values == null){
                throw new ArgumentNullException("values");
            }
            if(values.Length < 1){
                throw new ArgumentException("Argument 'values' must contain at least 1 value.");
            }

            // TODO:
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static ABNF_DecVal Parse(System.IO.StringReader reader)
        {
            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // dec-val =  "d" 1*DIGIT [ 1*("." 1*DIGIT) / ("-" 1*DIGIT) ]

            if(reader.Peek() != 'd'){
                throw new ParseException("Invalid ABNF 'dec-val' value '" + reader.ReadToEnd() + "'.");
            }

            // Eat 'd'.
            reader.Read();

            if(!char.IsNumber((char)reader.Peek())){
                throw new ParseException("Invalid ABNF 'dec-val' value '" + reader.ReadToEnd() + "'.");
            }

            ValueType     valueType = ValueType.Single;
            List<int>     values    = new List<int>();
            StringBuilder b         = new StringBuilder();
            while(true){
                // We reached end of string.
                if(reader.Peek() == -1){
                    // - or . without required 1 DIGIT.
                    if(b.Length == 0){
                        throw new ParseException("Invalid ABNF 'dec-val' value '" + reader.ReadToEnd() + "'.");
                    }
                    break;
                }
                else if(char.IsNumber((char)reader.Peek())){
                    b.Append((char)reader.Read());
                }
                // Concated value.
                else if(reader.Peek() == '.'){
                    // Range and conacted is not allowed to mix.
                    if(valueType == ValueType.Range){
                        throw new ParseException("Invalid ABNF 'dec-val' value '" + reader.ReadToEnd() + "'.");
                    }
                    if(b.Length == 0){
                        throw new ParseException("Invalid ABNF 'dec-val' value '" + reader.ReadToEnd() + "'.");
                    }

                    values.Add(Convert.ToInt32(b.ToString()));
                    b = new StringBuilder();
                    valueType = ValueType.Concated;

                    // Eat '.'.
                    reader.Read();
                }
                // Value range.
                else if(reader.Peek() == '-'){
                    // Range and conacted is not allowed to mix. Also multiple ranges not allowed.
                    if(valueType != ValueType.Single){
                        throw new ParseException("Invalid ABNF 'dec-val' value '" + reader.ReadToEnd() + "'.");
                    }
                    values.Add(Convert.ToInt32(b.ToString()));
                    b = new StringBuilder();
                    valueType = ValueType.Range;

                    // Eat '-'.
                    reader.Read();
                }
                // Not dec-val char, value reading completed.
                else{
                    // - or . without required 1 DIGIT.
                    if(b.Length == 0){
                        throw new ParseException("Invalid ABNF 'dec-val' value '" + reader.ReadToEnd() + "'.");
                    }
                    break;
                }
            }
            values.Add(Convert.ToInt32(b.ToString()));

            if(valueType == ValueType.Single){
                return new ABNF_DecVal(values[0],values[0]);
            }
            else if(valueType == ValueType.Concated){
                return new ABNF_DecVal(values.ToArray());
            }
            else{
                return new ABNF_DecVal(values[0],values[1]);
            }
        }


        #region Properties implementation

        #endregion
    }
}
