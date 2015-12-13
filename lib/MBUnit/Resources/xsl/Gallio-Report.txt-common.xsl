<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                xmlns:g="http://www.gallio.org/">

  <xsl:output method="text" encoding="utf-8"/>

  <xsl:param name="resourceRoot" select="''" />

  <xsl:template match="/">
    <xsl:apply-templates select="/g:report" />
  </xsl:template>

  <xsl:template match="g:report">
    <xsl:apply-templates select="." mode="results"/>
    <xsl:apply-templates select="." mode="annotations" />
    <xsl:apply-templates select="." mode="log" />
  </xsl:template>
  
  <!-- Results -->

  <xsl:template match="g:report" mode="results">
    <xsl:text>* Results: </xsl:text>
    <xsl:call-template name="format-statistics">
      <xsl:with-param name="statistics" select="g:testPackageRun/g:statistics" />
    </xsl:call-template>
		<xsl:text>&#xA;&#xA;</xsl:text>

    <xsl:variable name="testCases" select="g:testPackageRun/g:testStepRun/descendant-or-self::g:testStepRun[g:testStep/@isTestCase='true']" />
    
    <xsl:variable name="passed" select="$testCases[g:result/g:outcome/@status='passed']" />
    <xsl:variable name="failed" select="$testCases[g:result/g:outcome/@status='failed']" />
    <xsl:variable name="inconclusive" select="$testCases[g:result/g:outcome/@status='inconclusive']" />
    <xsl:variable name="skipped" select="$testCases[g:result/g:outcome/@status='skipped']" />

    <xsl:if test="$show-failed-tests">
      <xsl:apply-templates select="$failed" mode="results" />
    </xsl:if>
    
    <xsl:if test="$show-inconclusive-tests">
      <xsl:apply-templates select="$inconclusive" mode="results"/>
    </xsl:if>

    <xsl:if test="$show-passed-tests">
      <xsl:apply-templates select="$passed" mode="results"/>
    </xsl:if>

    <xsl:if test="$show-skipped-tests">
      <xsl:apply-templates select="$skipped" mode="results"/>
    </xsl:if>

    <xsl:text>&#xA;</xsl:text>
  </xsl:template>
  
  <xsl:template match="g:testStepRun" mode="results">
    <xsl:text>[</xsl:text>
    <xsl:choose>
      <xsl:when test="g:result/g:outcome/@category">
        <xsl:value-of select="g:result/g:outcome/@category"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="g:result/g:outcome/@status"/>
      </xsl:otherwise>
    </xsl:choose>
    <xsl:text>] </xsl:text>

    <xsl:variable name="kind" select="g:testStep/g:metadata/g:entry[@key='TestKind']/g:value" />
    <xsl:if test="$kind">
      <xsl:value-of select="$kind"/>
      <xsl:text> </xsl:text>
    </xsl:if>

    <xsl:value-of select="g:testStep/@fullName" />
    <xsl:text>&#xA;</xsl:text>
    <xsl:apply-templates select="g:testLog" mode="results"/>
    <xsl:text>&#xA;</xsl:text>

    <xsl:apply-templates select="g:children/g:testStepRun" mode="results"/>
  </xsl:template>

  <xsl:template match="g:testLog" mode="results">
    <xsl:apply-templates select="g:streams/g:stream/g:body"/>
  </xsl:template>

  <xsl:template match="g:body">
    <xsl:apply-templates select="g:contents" mode="block" />
  </xsl:template>

  <xsl:template match="g:contents" mode="block">
    <xsl:variable name="text">
      <xsl:apply-templates select="." mode="inline" />
    </xsl:variable>
    
    <xsl:call-template name="indent">
      <xsl:with-param name="text" select="$text"/>
    </xsl:call-template>
  </xsl:template>

  <xsl:template match="g:contents" mode="inline">
    <xsl:apply-templates select="child::node()[self::g:text or self::g:section or self::g:embed or self::g:marker]" />
  </xsl:template>

  <xsl:template match="g:text">
    <xsl:value-of select="text()"/>
  </xsl:template>

  <xsl:template match="g:section">
    <xsl:text>&#xA;</xsl:text>
    <xsl:value-of select="@name" />
    <xsl:text>&#xA;</xsl:text>
    <xsl:apply-templates select="g:contents" mode="block" />
  </xsl:template>

  <xsl:template match="g:marker">
    <xsl:apply-templates select="g:contents" mode="inline" />
  </xsl:template>

  <xsl:template match="g:embed">
    <xsl:text>&#xA;[Attachment: </xsl:text>
    <xsl:value-of select="@attachmentName"/>
    <xsl:text>]&#xA;</xsl:text>
  </xsl:template>

  <!-- Annotations -->
  
  <xsl:template match="g:report" mode="annotations">
    <xsl:variable name="annotations" select="g:testModel/g:annotations/g:annotation" />
    
    <xsl:if test="$annotations">
      <xsl:text>* Annotations:&#xA;&#xA;</xsl:text>
      <xsl:apply-templates select="$annotations[@type='error']"/>
      <xsl:apply-templates select="$annotations[@type='warning']"/>
      <xsl:apply-templates select="$annotations[@type='info']"/>
      <xsl:text>&#xA;</xsl:text>
    </xsl:if>
  </xsl:template>

  <xsl:template match="g:annotation">
    <xsl:call-template name="indent">
      <xsl:with-param name="text">
        <xsl:text>[</xsl:text>
        <xsl:value-of select="@type"/>
        <xsl:text>] </xsl:text>
        <xsl:value-of select="@message"/>
      </xsl:with-param>
      <xsl:with-param name="firstLinePrefix" select="''" />
    </xsl:call-template>

    <xsl:if test="g:codeLocation/@path">
      <xsl:call-template name="indent">
        <xsl:with-param name="text">
          <xsl:text>Location: </xsl:text>
          <xsl:call-template name="format-code-location">
            <xsl:with-param name="codeLocation" select="g:codeLocation" />
          </xsl:call-template>
        </xsl:with-param>
        <xsl:with-param name="secondLinePrefix" select="'    '" />
      </xsl:call-template>
    </xsl:if>

    <xsl:if test="g:codeReference/@assembly">
      <xsl:call-template name="indent">
        <xsl:with-param name="text">
          <xsl:text>Reference: </xsl:text>
          <xsl:call-template name="format-code-reference">
            <xsl:with-param name="codeReference" select="g:codeReference" />
          </xsl:call-template>
        </xsl:with-param>
        <xsl:with-param name="secondLinePrefix" select="'    '" />
      </xsl:call-template>
    </xsl:if>

    <xsl:if test="@details">
      <xsl:call-template name="indent">
        <xsl:with-param name="text">
          <xsl:text>Details: </xsl:text>
          <xsl:value-of select="@details"/>
        </xsl:with-param>
        <xsl:with-param name="secondLinePrefix" select="'    '" />
      </xsl:call-template>
    </xsl:if>

    <xsl:text>&#xA;</xsl:text>
  </xsl:template>

  <!-- Log -->
  
  <xsl:template match="g:report" mode="log">
    <xsl:variable name="logEntries" select="g:logEntries/g:logEntry[@severity != 'debug']"/>
    
    <xsl:if test="$logEntries">
      <xsl:text>* Diagnostic Log:&#xA;&#xA;</xsl:text>
      <xsl:apply-templates select="$logEntries"/>
      <xsl:text>&#xA;</xsl:text>
    </xsl:if>
  </xsl:template>

  <xsl:template match="g:logEntry">
    <xsl:call-template name="indent">
      <xsl:with-param name="text">
        <xsl:text>[</xsl:text>
        <xsl:value-of select="@severity"/>
        <xsl:text>] </xsl:text>
        <xsl:value-of select="@message"/>
      </xsl:with-param>
      <xsl:with-param name="firstLinePrefix" select="''" />
    </xsl:call-template>

    <xsl:if test="@details">
      <xsl:call-template name="indent">
        <xsl:with-param name="text">
          <xsl:value-of select="@details"/>
        </xsl:with-param>
        <xsl:with-param name="secondLinePrefix" select="'    '" />
      </xsl:call-template>
    </xsl:if>

    <xsl:text>&#xA;</xsl:text>
  </xsl:template>
  
  <!-- -->

  <xsl:template match="*">
  </xsl:template>
  
  <!-- Include the common report template -->
  <xsl:include href="Gallio-Report.common.xsl" />  
</xsl:stylesheet>