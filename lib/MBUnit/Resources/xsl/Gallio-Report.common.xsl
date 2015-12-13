<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                xmlns:g="http://www.gallio.org/"
                xmlns="http://www.w3.org/1999/xhtml">
  <!-- Common utility functions -->
  
  <!-- Formats a statistics line like 5 run, 3 passed, 2 failed (1 error), 0 inconclusive, 2 skipped -->
  <xsl:template name="format-statistics">
    <xsl:param name="statistics" />
       
		<xsl:value-of select="$statistics/@runCount"/>    
    <xsl:text> run, </xsl:text>
    
    <xsl:value-of select="$statistics/@passedCount"/>
    <xsl:text> passed</xsl:text>
    <xsl:call-template name="format-statistics-category-counts">
      <xsl:with-param name="statistics" select="$statistics" />
      <xsl:with-param name="status">passed</xsl:with-param>
    </xsl:call-template>
    <xsl:text>, </xsl:text>
    
    <xsl:value-of select="$statistics/@failedCount"/>
    <xsl:text> failed</xsl:text>
    <xsl:call-template name="format-statistics-category-counts">
      <xsl:with-param name="statistics" select="$statistics" />
      <xsl:with-param name="status">failed</xsl:with-param>
    </xsl:call-template>
    <xsl:text>, </xsl:text>

    <xsl:value-of select="$statistics/@inconclusiveCount"/>
    <xsl:text> inconclusive</xsl:text>
    <xsl:call-template name="format-statistics-category-counts">
      <xsl:with-param name="statistics" select="$statistics" />
      <xsl:with-param name="status">inconclusive</xsl:with-param>
    </xsl:call-template>
    <xsl:text>, </xsl:text>
    
    <xsl:value-of select="$statistics/@skippedCount"/>
    <xsl:text> skipped</xsl:text>
    <xsl:call-template name="format-statistics-category-counts">
      <xsl:with-param name="statistics" select="$statistics" />
      <xsl:with-param name="status">skipped</xsl:with-param>
    </xsl:call-template>
  </xsl:template>
  
  <xsl:template name="format-statistics-category-counts">
    <xsl:param name="statistics" />
    <xsl:param name="status" />
    
    <xsl:variable name="outcomeSummaries" select="$statistics/g:outcomeSummaries/g:outcomeSummary[g:outcome/@status=$status and g:outcome/@category]" />
    
    <xsl:if test="$outcomeSummaries">
      <xsl:text> (</xsl:text>
        <xsl:for-each select="$outcomeSummaries">
          <xsl:sort data-type="text" order="ascending" select="g:outcome/@category"/>
          
          <xsl:if test="position() != 1"><xsl:text>, </xsl:text></xsl:if>
          <xsl:value-of select="@count"/>
          <xsl:text> </xsl:text>
          <xsl:value-of select="g:outcome/@category"/>
        </xsl:for-each>
      <xsl:text>)</xsl:text>
    </xsl:if>
  </xsl:template>
  
  <!-- Formats a CodeLocation -->
  <xsl:template name="format-code-location">
    <xsl:param name="codeLocation" />
    
    <xsl:choose>
      <xsl:when test="$codeLocation/@path">
        <xsl:value-of select="$codeLocation/@path"/>
        <xsl:if test="$codeLocation/@line">
          <xsl:text>(</xsl:text>
          <xsl:value-of select="$codeLocation/@line"/>
          <xsl:if test="$codeLocation/@column">
            <xsl:text>,</xsl:text>
            <xsl:value-of select="$codeLocation/@column"/>
          </xsl:if>
          <xsl:text>)</xsl:text>
        </xsl:if>
      </xsl:when>
      <xsl:otherwise>
        <xsl:text>(unknown)</xsl:text>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Formats a CodeReference -->
  <xsl:template name="format-code-reference">
    <xsl:param name="codeReference" />
    
    <xsl:if test="$codeReference/@parameter">
      <xsl:text>Parameter </xsl:text>
      <xsl:value-of select="$codeReference/@parameter"/>
      <xsl:text> of </xsl:text>
    </xsl:if>
    
    <xsl:choose>
      <xsl:when test="$codeReference/@type">
        <xsl:value-of select="$codeReference/@type"/>
        <xsl:if test="$codeReference/@member">
          <xsl:text>.</xsl:text>
          <xsl:value-of select="$codeReference/@member"/>
        </xsl:if>
      </xsl:when>
      <xsl:when test="$codeReference/@namespace">
        <xsl:value-of select="$codeReference/@namespace"/>
      </xsl:when>
    </xsl:choose>
    
    <xsl:if test="$codeReference/@assembly">
      <xsl:if test="$codeReference/@namespace">
        <xsl:text>, </xsl:text>
      </xsl:if>
      <xsl:value-of select="$codeReference/@assembly"/>
    </xsl:if>
  </xsl:template>
  
  <!-- Creates an aggregate statistics summary from a test instance run and its descendants -->
  <xsl:template name="aggregate-statistics">
    <xsl:param name="testStepRun" />

    <xsl:variable name="testCaseResults" select="$testStepRun/descendant-or-self::g:testStepRun[g:testStep/@isTestCase='true']/g:result" />
    <xsl:variable name="testCaseOutcomes" select="$testCaseResults/g:outcome" />
    
    <xsl:variable name="skippedOutcomes" select="$testCaseOutcomes[@status = 'skipped']" />
    <xsl:variable name="passedOutcomes" select="$testCaseOutcomes[@status = 'passed']" />
    <xsl:variable name="inconclusiveOutcomes" select="$testCaseOutcomes[@status = 'inconclusive']" />
    <xsl:variable name="failedOutcomes" select="$testCaseOutcomes[@status = 'failed']" />
    
    <xsl:variable name="skippedCount" select="count($skippedOutcomes)"/>
    <xsl:variable name="passedCount" select="count($passedOutcomes)"/>
    <xsl:variable name="inconclusiveCount" select="count($inconclusiveOutcomes)"/>
    <xsl:variable name="failedCount" select="count($failedOutcomes)"/>

    <g:statistics>
      <xsl:attribute name="duration"><xsl:value-of select="$testStepRun/g:result/@duration"/></xsl:attribute>
      <xsl:attribute name="assertCount"><xsl:value-of select="$testStepRun/g:result/@assertCount"/></xsl:attribute>
      
      <xsl:attribute name="skippedCount"><xsl:value-of select="$skippedCount"/></xsl:attribute>
      <xsl:attribute name="passedCount"><xsl:value-of select="$passedCount"/></xsl:attribute>
      <xsl:attribute name="inconclusiveCount"><xsl:value-of select="$inconclusiveCount"/></xsl:attribute>
      <xsl:attribute name="failedCount"><xsl:value-of select="$failedCount"/></xsl:attribute>
      
      <xsl:attribute name="runCount"><xsl:value-of select="$passedCount + $inconclusiveCount + $failedCount"/></xsl:attribute>
      
      <g:outcomeSummaries>
        <xsl:call-template name="aggregate-statistics-outcome-summaries">
          <xsl:with-param name="status">skipped</xsl:with-param>
          <xsl:with-param name="outcomes" select="$skippedOutcomes" />
        </xsl:call-template>
        <xsl:call-template name="aggregate-statistics-outcome-summaries">
          <xsl:with-param name="status">passed</xsl:with-param>
          <xsl:with-param name="outcomes" select="$passedOutcomes" />
        </xsl:call-template>
        <xsl:call-template name="aggregate-statistics-outcome-summaries">
          <xsl:with-param name="status">inconclusive</xsl:with-param>
          <xsl:with-param name="outcomes" select="$inconclusiveOutcomes" />
        </xsl:call-template>
        <xsl:call-template name="aggregate-statistics-outcome-summaries">
          <xsl:with-param name="status">failed</xsl:with-param>
          <xsl:with-param name="outcomes" select="$failedOutcomes" />
        </xsl:call-template>
      </g:outcomeSummaries>
    </g:statistics>
  </xsl:template>
  
  <xsl:key name="outcome-category" match="g:outcome" use="@category" />
  <xsl:template name="aggregate-statistics-outcome-summaries">
    <xsl:param name="status" />
    <xsl:param name="outcomes" />
    
    <xsl:for-each select="$outcomes[generate-id() = generate-id(key('outcome-category', @category)[1])]">
      <xsl:variable name="category" select="@category" />
      <g:outcomeSummary count="{count($outcomes[@category = $category])}">
        <g:outcome status="{$status}" category="{$category}" />
      </g:outcomeSummary>
    </xsl:for-each>
  </xsl:template>
  
  <!-- Indents text using the specified prefix -->
  <xsl:template name="indent">
    <xsl:param name="text" />
    <xsl:param name="firstLinePrefix" select="'  '" />
    <xsl:param name="otherLinePrefix" select="'  '" />
    <xsl:param name="trailingNewline" select="1" />

    <xsl:if test="$text!=''">
      <xsl:call-template name="indent-recursive">
        <xsl:with-param name="text" select="translate($text, '&#9;&#xA;&#xD;', ' &#xA;')" />
        <xsl:with-param name="firstLinePrefix" select="$firstLinePrefix" />
        <xsl:with-param name="otherLinePrefix" select="$otherLinePrefix" />
        <xsl:with-param name="trailingNewline" select="$trailingNewline" />
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

  <xsl:template name="indent-recursive">
    <xsl:param name="text" />
    <xsl:param name="firstLinePrefix" />
    <xsl:param name="otherLinePrefix" />
    <xsl:param name="trailingNewline" />

    <xsl:choose>
      <xsl:when test="contains($text, '&#xA;')">
        <xsl:value-of select="$firstLinePrefix"/>
        <xsl:value-of select="substring-before($text, '&#xA;')"/>
        <xsl:text>&#xA;</xsl:text>
        <xsl:call-template name="indent-recursive">
          <xsl:with-param name="text" select="substring-after($text, '&#xA;')" />
          <xsl:with-param name="firstLinePrefix" select="$otherLinePrefix" />
          <xsl:with-param name="otherLinePrefix" select="$otherLinePrefix" />
          <xsl:with-param name="trailingNewline" select="$trailingNewline" />
        </xsl:call-template>
      </xsl:when>
      <!-- Note: when we are not adding a trailing new line, we must be careful
           to include the leading prefix space even when the text line is empty
           because subsequently appended text will assume that the current
           line is already indented appropriately -->
      <xsl:when test="$text!='' or not($trailingNewline)">
        <xsl:value-of select="$firstLinePrefix"/>
        <xsl:value-of select="$text"/>
        <xsl:if test="$trailingNewline">
          <xsl:text>&#xA;</xsl:text>
        </xsl:if>
      </xsl:when>
    </xsl:choose>
  </xsl:template>

  <!-- Prints text with <br/> at line breaks.
       Also introduces <wbr/> word break characters every so often if no natural breakpoints appear.
       Some of the soft breaks are useful, but the forced break at 20 chars is intended to work around
       limitations in browsers that do not support the word-wrap:break-all CSS3 property.
  -->
  <xsl:template name="print-text-with-breaks">
    <xsl:param name="text" />
    <xsl:param name="count" select="0" />
    
    <xsl:if test="$text">
      <xsl:variable name="char" select="substring($text, 1, 1)"/>
      
      <xsl:choose>
        <!-- natural word breaks -->
        <xsl:when test="$char = ' '">
          <!-- Always replace spaces by non-breaking spaces followed by word-breaks to ensure that
               text can reflow without actually consuming the space.  Without this detail
               it can happen that spaces that are supposed to be highligted (perhaps as part
               of a marker for a diff) will instead vanish when the text reflow occurs, giving
               a false impression of the content. -->
          <xsl:text>&#160;</xsl:text>
          <wbr/>
          <xsl:call-template name="print-text-with-breaks">
            <xsl:with-param name="text" select="substring($text, 2)" />
            <xsl:with-param name="count" select="0" />
          </xsl:call-template>
        </xsl:when>

        <!-- line breaks -->
        <xsl:when test="$char = '&#10;'">
          <br/>
          <xsl:call-template name="print-text-with-breaks">
            <xsl:with-param name="text" select="substring($text, 2)" />
            <xsl:with-param name="count" select="0" />
          </xsl:call-template>
        </xsl:when>
        
        <!-- characters to break before -->
        <xsl:when test="$char = '.' or $char = '/' or $char = '\' or $char = ':' or $char = '(' or $char = '&lt;' or $char = '[' or $char = '{' or $char = '_'">
          <wbr/>
          <xsl:value-of select="$char"/>
          <xsl:call-template name="print-text-with-breaks">
            <xsl:with-param name="text" select="substring($text, 2)" />
            <xsl:with-param name="count" select="1" />
          </xsl:call-template>
        </xsl:when>
        
        <!-- characters to break after -->
        <xsl:when test="$char = ')' or $char = '>' or $char = ']' or $char = '}'">
          <xsl:value-of select="$char"/>
          <wbr/>
          <xsl:call-template name="print-text-with-breaks">
            <xsl:with-param name="text" select="substring($text, 2)" />
            <xsl:with-param name="count" select="0" />
          </xsl:call-template>
        </xsl:when>
        
        <!-- other characters -->
        <xsl:when test="$count = 19">
          <xsl:value-of select="$char"/>
          <wbr/>
          <xsl:call-template name="print-text-with-breaks">
            <xsl:with-param name="text" select="substring($text, 2)" />
            <xsl:with-param name="count" select="0" />
          </xsl:call-template>
        </xsl:when>
        
        <xsl:otherwise>
          <xsl:value-of select="$char"/>
          <xsl:call-template name="print-text-with-breaks">
            <xsl:with-param name="text" select="substring($text, 2)" />
            <xsl:with-param name="count" select="$count + 1" />
          </xsl:call-template>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:if>
  </xsl:template>
  
  <!-- Pretty print date time values -->
  <xsl:template name="format-datetime">
    <xsl:param name="datetime" />
    <xsl:value-of select="substring($datetime, 12, 8)" />, <xsl:value-of select="substring($datetime, 1, 10)" />
  </xsl:template>
  
  <!-- Namespace stripping adapted from http://www.xml.com/pub/a/2004/05/05/tr.html -->
  <xsl:template name="strip-namespace">
    <xsl:param name="nodes" />
    <xsl:apply-templates select="msxsl:node-set($nodes)" mode="strip-namespace" />
  </xsl:template>
  
  <xsl:template match="*" mode="strip-namespace">
    <xsl:element name="{local-name()}" namespace="">
      <xsl:apply-templates select="@*|node()" mode="strip-namespace"/>
    </xsl:element>
  </xsl:template>

  <xsl:template match="@*" mode="strip-namespace">
    <xsl:attribute name="{local-name()}" namespace="">
      <xsl:value-of select="."/>
    </xsl:attribute>
  </xsl:template>

  <xsl:template match="processing-instruction()|comment()" mode="strip-namespace">
    <xsl:copy>
      <xsl:apply-templates select="node()" mode="strip-namespace"/>
    </xsl:copy>
  </xsl:template>  
  
  <!-- Converting paths to URIs -->
  <xsl:template name="path-to-uri">
    <xsl:param name="path" />    
    <xsl:if test="$path != ''">
      <xsl:choose>
        <xsl:when test="starts-with($path, '\')">/</xsl:when>
        <xsl:when test="starts-with($path, ' ')">%20</xsl:when>
        <xsl:when test="starts-with($path, '%')">%25</xsl:when>
        <xsl:otherwise><xsl:value-of select="substring($path, 1, 1)"/></xsl:otherwise>
      </xsl:choose>
      <xsl:call-template name="path-to-uri">
        <xsl:with-param name="path" select="substring($path, 2)" />
      </xsl:call-template>
    </xsl:if>
  </xsl:template>
</xsl:stylesheet>
