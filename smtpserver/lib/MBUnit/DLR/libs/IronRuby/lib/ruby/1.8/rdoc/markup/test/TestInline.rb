require "test/unit"

$:.unshift "../../.."

require "rdoc/markup/simple_markup/inline"

class TestInline < Test::Unit::TestCase


  def setup
    @am = SM::AttributeManager.new

    @bold_on  = @am.changed_attribute_by_name([], [:BOLD])
    @bold_off = @am.changed_attribute_by_name([:BOLD], [])
    
    @tt_on    = @am.changed_attribute_by_name([], [:TT])
    @tt_off   = @am.changed_attribute_by_name([:TT], [])
    
    @em_on    = @am.changed_attribute_by_name([], [:EM])
    @em_off   = @am.changed_attribute_by_name([:EM], [])
    
    @bold_em_on   = @am.changed_attribute_by_name([], [:BOLD] | [:EM])
    @bold_em_off  = @am.changed_attribute_by_name([:BOLD] | [:EM], [])
    
    @em_then_bold = @am.changed_attribute_by_name([:EM], [:EM] | [:BOLD])
    
    @em_to_bold   = @am.changed_attribute_by_name([:EM], [:BOLD])
    
    @am.add_word_pair("{", "}", :WOMBAT)
    @wombat_on    = @am.changed_attribute_by_name([], [:WOMBAT])
    @wombat_off   = @am.changed_attribute_by_name([:WOMBAT], [])
  end

  def crossref(text)
    [ @am.changed_attribute_by_name([], [:CROSSREF] | [:_SPECIAL_]),
      SM::Special.new(33, text),
      @am.changed_attribute_by_name([:CROSSREF] | [:_SPECIAL_], [])
    ]
  end

  def test_special
    # class names, variable names, file names, or instance variables
    @am.add_special(/(
                       \b([A-Z]\w+(::\w+)*)
                       | \#\w+[!?=]?
                       | \b\w+([_\/\.]+\w+)+[!?=]?
                      )/x, 
                    :CROSSREF)
    
    assert_equal(["cat"], @am.flow("cat"))

    assert_equal(["cat ", crossref("#fred"), " dog"].flatten,
                  @am.flow("cat #fred dog"))

    assert_equal([crossref("#fred"), " dog"].flatten,
                  @am.flow("#fred dog"))

    assert_equal(["cat ", crossref("#fred")].flatten, @am.flow("cat #fred"))
  end

  def test_basic
    assert_equal(["cat"], @am.flow("cat"))

    assert_equal(["cat ", @bold_on, "and", @bold_off, " dog"],
                  @am.flow("cat *and* dog"))

    assert_equal(["cat ", @bold_on, "AND", @bold_off, " dog"],
                  @am.flow("cat *AND* dog"))

    assert_equal(["cat ", @em_on, "And", @em_off, " dog"],
                  @am.flow("cat _And_ dog"))

    assert_equal(["cat *and dog*"], @am.flow("cat *and dog*"))

    assert_equal(["*cat and* dog"], @am.flow("*cat and* dog"))

    assert_equal(["cat *and ", @bold_on, "dog", @bold_off],
                  @am.flow("cat *and *dog*"))

    assert_equal(["cat ", @em_on, "and", @em_off, " dog"],
                  @am.flow("cat _and_ dog"))

    assert_equal(["cat_and_dog"],
                  @am.flow("cat_and_dog"))

    assert_equal(["cat ", @tt_on, "and", @tt_off, " dog"],
                  @am.flow("cat +and+ dog"))

    assert_equal(["cat ", @bold_on, "a_b_c", @bold_off, " dog"],
                  @am.flow("cat *a_b_c* dog"))

    assert_equal(["cat __ dog"],
                  @am.flow("cat __ dog"))

    assert_equal(["cat ", @em_on, "_", @em_off, " dog"],
                  @am.flow("cat ___ dog"))

  end

  def test_combined
    assert_equal(["cat ", @em_on, "and", @em_off, " ", @bold_on, "dog", @bold_off],
                  @am.flow("cat _and_ *dog*"))

    assert_equal(["cat ", @em_on, "a__nd", @em_off, " ", @bold_on, "dog", @bold_off], 
                  @am.flow("cat _a__nd_ *dog*"))
  end

  def test_html_like
    assert_equal(["cat ", @tt_on, "dog", @tt_off], @am.flow("cat <tt>dog</Tt>"))

    assert_equal(["cat ", @em_on, "and", @em_off, " ", @bold_on, "dog", @bold_off], 
                  @am.flow("cat <i>and</i> <B>dog</b>"))
    
    assert_equal(["cat ", @em_on, "and ", @em_then_bold, "dog", @bold_em_off], 
                  @am.flow("cat <i>and <B>dog</B></I>"))
    
    assert_equal(["cat ", @em_on, "and ", @em_to_bold, "dog", @bold_off], 
                  @am.flow("cat <i>and </i><b>dog</b>"))
    
    assert_equal(["cat ", @em_on, "and ", @em_to_bold, "dog", @bold_off], 
                  @am.flow("cat <i>and <b></i>dog</b>"))
    
    assert_equal([@tt_on, "cat", @tt_off, " ", @em_on, "and ", @em_to_bold, "dog", @bold_off], 
                  @am.flow("<tt>cat</tt> <i>and <b></i>dog</b>"))

    assert_equal(["cat ", @em_on, "and ", @em_then_bold, "dog", @bold_em_off], 
                  @am.flow("cat <i>and <b>dog</b></i>"))
    
    assert_equal(["cat ", @bold_em_on, "and", @bold_em_off, " dog"], 
                  @am.flow("cat <i><b>and</b></i> dog"))
    
    
  end

  def test_protect
    assert_equal(['cat \\ dog'], @am.flow('cat \\ dog'))

    assert_equal(["cat <tt>dog</Tt>"], @am.flow("cat \\<tt>dog</Tt>"))

    assert_equal(["cat ", @em_on, "and", @em_off, " <B>dog</b>"], 
                  @am.flow("cat <i>and</i> \\<B>dog</b>"))
    
    assert_equal(["*word* or <b>text</b>"], @am.flow("\\*word* or \\<b>text</b>"))

    assert_equal(["_cat_", @em_on, "dog", @em_off], 
                  @am.flow("\\_cat_<i>dog</i>"))
  end

  def test_adding
    assert_equal(["cat ", @wombat_on, "and", @wombat_off, " dog" ],
                  @am.flow("cat {and} dog"))
#    assert_equal(["cat {and} dog" ], @am.flow("cat \\{and} dog"))
  end
end
