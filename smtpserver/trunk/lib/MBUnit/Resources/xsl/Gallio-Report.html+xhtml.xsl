<?xml version="1.0" encoding="utf-8"?>
<!--
  This stylesheet is used for all HTML detail reports.
  It can be rendered in any of the following modes.
  
  Document / Fragment:
      A report can either be rendered a self-contained document or as
      a fragment meant to be included in another document.
  
  HTML / XHTML:
      A report can either be rendered in HTML or in XHTML syntax.
      
  One very important characteristic of the report is that while it uses JavaScript,
  it does not require it.  All of the report's contents may be accessed without error
  or serious inconvenience even with JavaScript disabled.  This is extremely important
  for Visual Studio integration since the IE browser prevents execution of scripts
  in local files by default.
-->
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                xmlns:g="http://www.gallio.org/"
                xmlns="http://www.w3.org/1999/xhtml">
  <xsl:template match="g:report" mode="xhtml-document">
    <xsl:param name="contentType" select="text/xhtml+xml" />
    
    <html xml:lang="en" lang="en" dir="ltr">
      <xsl:comment> saved from url=(0014)about:internet </xsl:comment><xsl:text>&#13;&#10;</xsl:text>
      <head>
        <meta http-equiv="Content-Type" content="{$contentType}; charset=utf-8" />
        <title>Gallio Test Report</title>
        <link rel="stylesheet" type="text/css" href="{$cssDir}Gallio-Report.css" />
        <link rel="stylesheet" type="text/css" href="{$cssDir}Gallio-Report.generated.css" />
        <script type="text/javascript" src="{$jsDir}Gallio-Report.js" />
        <xsl:if test="g:testPackageRun//g:testStepRun/g:testLog/g:attachments/g:attachment[@contentType='video/x-flv']">
          <script type="text/javascript" src="{$jsDir}swfobject.js" />
        </xsl:if>
        <style type="text/css">
html
{
	overflow: auto;
}
        </style>
      </head>
      <body class="gallio-report">
        <xsl:apply-templates select="." mode="xhtml-body" />
      </body>
      <script type="text/javascript">reportLoaded();</script>
    </html>
  </xsl:template>
  
  <xsl:template match="g:report" mode="html-document">
    <xsl:call-template name="strip-namespace">
      <xsl:with-param name="nodes">
        <xsl:apply-templates select="." mode="xhtml-document">
          <xsl:with-param name="contentType" select="text/html" />
        </xsl:apply-templates>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <xsl:template match="g:report" mode="xhtml-fragment">
    <div class="gallio-report">
      <!-- Technically a link element should not appear outside of the "head"
           but most browsers tolerate it and this gives us better out of the box
           support in embedded environments like CCNet since no changes need to
           be made to the stylesheets of the containing application.
      -->
      <link rel="stylesheet" type="text/css" href="{$cssDir}Gallio-Report.css" />
      <link rel="stylesheet" type="text/css" href="{$cssDir}Gallio-Report.generated.css" />
      <style type="text/css">
html
{
	margin: 0px 0px 0px 0px;
	padding: 0px 17px 0px 0px;
	overflow: auto;
}
      </style>
      <script type="text/javascript" src="{$jsDir}Gallio-Report.js" />
      <xsl:if test="g:testPackageRun//g:testStepRun/g:testLog/g:attachments/g:attachment[@contentType='video/x-flv']">
        <script type="text/javascript" src="{$jsDir}swfobject.js" />
      </xsl:if>

      <xsl:apply-templates select="." mode="xhtml-body" />
    </div>
    <script type="text/javascript">reportLoaded();</script>
  </xsl:template>

  <xsl:template match="g:report" mode="html-fragment">
    <xsl:call-template name="strip-namespace">
      <xsl:with-param name="nodes"><xsl:apply-templates select="." mode="xhtml-fragment" /></xsl:with-param>
    </xsl:call-template>
  </xsl:template>
  
  <xsl:template match="g:report" mode="xhtml-body">
    <div id="Header" class="header">
      <div class="header-image"></div>
    </div>
    <div id="Navigator" class="navigator">
      <xsl:apply-templates select="g:testPackageRun" mode="navigator" />
    </div>
    <div id="Content" class="content">
      <iframe id="_asyncLoadFrame" src="about:blank" style="border: 0px; margin: 0px 0px 0px 0px; padding: 0px 0px 0px 0px; width: 0px; height: 0px; display: none;" onload="_asyncLoadFrameOnLoad()" />
      
      <xsl:apply-templates select="g:testPackageRun" mode="statistics" />
      <xsl:apply-templates select="g:testPackage" mode="files" />
      <xsl:apply-templates select="g:testModel/g:annotations" mode="annotations"/>
      <xsl:apply-templates select="g:testPackageRun" mode="summary"/>
      <xsl:apply-templates select="g:testPackageRun" mode="details"/>
      <xsl:apply-templates select="g:logEntries" mode="log"/>
    </div>
  </xsl:template>
  
  <xsl:template match="g:testPackage" mode="files">
    <div id="Files" class="section">
      <h2>Files</h2>
      <div class="section-content">
        <ul>
          <xsl:for-each select="g:files/g:file">
            <li><xsl:call-template name="print-text-with-breaks"><xsl:with-param name="text" select="text()" /></xsl:call-template></li>
          </xsl:for-each>
        </ul>
      </div>
    </div>
  </xsl:template>

  <xsl:template match="g:annotations" mode="annotations">
    <xsl:if test="g:annotation">
      <div id="Annotations" class="section">
        <h2>Annotations</h2>
        <div class="section-content">
          <ul>
            <xsl:apply-templates select="g:annotation[@type='error']" mode="annotations"/>
            <xsl:apply-templates select="g:annotation[@type='warning']" mode="annotations"/>
            <xsl:apply-templates select="g:annotation[@type='info']" mode="annotations"/>
          </ul>
        </div>
      </div>
    </xsl:if>
  </xsl:template>

  <xsl:template match="g:annotation" mode="annotations">
    <li>
      <xsl:attribute name="class">annotation annotation-type-<xsl:value-of select="@type"/></xsl:attribute>
      <div class="annotation-message">
        <xsl:text>[</xsl:text><xsl:value-of select="@type"/><xsl:text>] </xsl:text>
        <xsl:call-template name="print-text-with-breaks"><xsl:with-param name="text" select="@message" /></xsl:call-template>
      </div>
      
      <xsl:if test="g:codeLocation/@path">
        <div class="annotation-location">
          <xsl:text>Location: </xsl:text>
          <xsl:call-template name="code-location-link">
            <xsl:with-param name="path" select="g:codeLocation/@path" />
            <xsl:with-param name="line" select="g:codeLocation/@line" />
            <xsl:with-param name="column" select="g:codeLocation/@column" />
            <xsl:with-param name="content">
              <xsl:call-template name="format-code-location">
                <xsl:with-param name="codeLocation" select="g:codeLocation" />
              </xsl:call-template>
            </xsl:with-param>
          </xsl:call-template>
        </div>
      </xsl:if>
      
      <xsl:if test="g:codeReference/@assembly">
        <div class="annotation-reference">
          <xsl:text>Reference: </xsl:text>
          <xsl:call-template name="format-code-reference"><xsl:with-param name="codeReference" select="g:codeReference" /></xsl:call-template>
        </div>
      </xsl:if>
      
      <xsl:if test="@details">
        <div class="annotation-details">
          <xsl:text>Details: </xsl:text>
          <xsl:call-template name="print-text-with-breaks"><xsl:with-param name="text" select="@details" /></xsl:call-template>
        </div>
      </xsl:if>
    </li>
  </xsl:template>
  
  <xsl:template match="g:testPackageRun" mode="navigator">
    <xsl:variable name="box-label"><xsl:call-template name="format-statistics"><xsl:with-param name="statistics" select="g:statistics" /></xsl:call-template></xsl:variable>
    <a href="#Statistics" title="{$box-label}">
      <xsl:attribute name="class">navigator-box <xsl:call-template name="status-from-statistics"><xsl:with-param name="statistics" select="g:statistics" /></xsl:call-template></xsl:attribute>
    </a>

    <div class="navigator-stripes">
      <xsl:for-each select="descendant::g:testStepRun">
        <xsl:variable name="status" select="g:result/g:outcome/@status"/>
        <xsl:if test="$status != 'passed' and (g:testStep/@isTestCase = 'true' or not(g:children/g:testStepRun))">
          <xsl:variable name="stripe-label"><xsl:value-of select="g:testStep/@name"/><xsl:text> </xsl:text><xsl:value-of select="$status"/>.</xsl:variable>
          <a href="#testStepRun-{g:testStep/@id}" style="top:{position() * 98 div last() + 1}%" class="status-{$status}" title="{$stripe-label}">
            <xsl:attribute name="onclick">
              <xsl:text>expand([</xsl:text>
              <xsl:for-each select="ancestor-or-self::g:testStepRun">
                <xsl:if test="position() != 1">
                  <xsl:text>,</xsl:text>
                </xsl:if>
                <xsl:text>'detailPanel-</xsl:text>
                <xsl:value-of select="g:testStep/@id"/>
                <xsl:text>'</xsl:text>
              </xsl:for-each>
              <xsl:text>]);</xsl:text>
            </xsl:attribute>
          </a>
        </xsl:if>
      </xsl:for-each>
    </div>
  </xsl:template>
  
  <xsl:template match="g:testPackageRun" mode="statistics">
    <div id="Statistics" class="section">
      <h2>Statistics</h2>
      <div class="section-content">
        <table class="statistics-table">
          <tr>
            <td class="statistics-label-cell">Start time:</td>
            <td><xsl:call-template name="format-datetime"><xsl:with-param name="datetime" select="@startTime" /></xsl:call-template></td>
          </tr>
          <tr class="alternate-row">
            <td class="statistics-label-cell">End time:</td>
            <td><xsl:call-template name="format-datetime"><xsl:with-param name="datetime" select="@endTime" /></xsl:call-template></td>
          </tr>
          <xsl:apply-templates select="g:statistics" />
        </table>
      </div>
    </div>
  </xsl:template>

  <xsl:template match="g:statistics">
    <tr>
      <td class="statistics-label-cell">Tests:</td>
      <td><xsl:value-of select="@testCount" /> (<xsl:value-of select="@stepCount" /> steps)</td>
    </tr>
    <tr class="alternate-row">
      <td class="statistics-label-cell">Results:</td>
      <td><xsl:call-template name="format-statistics"><xsl:with-param name="statistics" select="." /></xsl:call-template></td>
    </tr>
    <tr>
      <td class="statistics-label-cell">Duration:</td>
      <td><xsl:value-of select="format-number(@duration, '0.00')" />s</td>
    </tr>
    <tr class="alternate-row">
      <td class="statistics-label-cell">Assertions:</td>
      <td><xsl:value-of select="@assertCount" /></td>
    </tr>
  </xsl:template>

  <xsl:template match="g:testPackageRun" mode="summary">
    <div id="Summary" class="section">
      <h2>Summary<xsl:if test="$condensed"> (Condensed)</xsl:if></h2>
      <div class="section-content">
        <xsl:choose>
          <xsl:when test="g:testStepRun/g:children/g:testStepRun">
            <xsl:choose>
              <xsl:when test="not($condensed) or g:testStepRun/g:result/g:outcome/@status!='passed'">
                <ul>
                  <xsl:apply-templates select="g:testStepRun/g:children/g:testStepRun" mode="summary" />
                </ul>
              </xsl:when>
              <xsl:otherwise>
                <em>All tests passed.</em>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:when>
          <xsl:otherwise>
            <em>This report does not contain any test runs.</em>
          </xsl:otherwise>
        </xsl:choose>
      </div>
    </div>
  </xsl:template>

  <xsl:template match="g:testStepRun" mode="summary">
    <xsl:variable name="id" select="g:testStep/@id" />
    
    <xsl:if test="g:testStep/@isTestCase='false' and (not($condensed) or g:result/g:outcome/@status!='passed')">
      <xsl:variable name="statisticsRaw">
        <xsl:call-template name="aggregate-statistics">
          <xsl:with-param name="testStepRun" select="." />
        </xsl:call-template>
      </xsl:variable>
      <xsl:variable name="statistics" select="msxsl:node-set($statisticsRaw)/g:statistics" />

      <xsl:variable name="metadataEntries" select="g:testStep/g:metadata/g:entry" />
      <xsl:variable name="kind" select="$metadataEntries[@key='TestKind']/g:value" />

      <li>
        <span>
          <xsl:choose>
            <xsl:when test="g:children/g:testStepRun/g:testStep/@isTestCase='false'">
              <xsl:call-template name="toggle">
                <xsl:with-param name="href">summaryPanel-<xsl:value-of select="$id"/></xsl:with-param>
              </xsl:call-template>
            </xsl:when>
            <xsl:otherwise>
              <xsl:call-template name="toggle-stop" />
            </xsl:otherwise>
          </xsl:choose>

          <xsl:call-template name="icon">
            <xsl:with-param name="kind" select="$kind" />
          </xsl:call-template>

          <a class="crossref" href="#testStepRun-{$id}">
            <xsl:attribute name="onclick">
              <xsl:text>expand([</xsl:text>
              <xsl:for-each select="ancestor-or-self::g:testStepRun">
                <xsl:if test="position() != 1">
                  <xsl:text>,</xsl:text>
                </xsl:if>
                <xsl:text>'detailPanel-</xsl:text>
                <xsl:value-of select="g:testStep/@id"/>
                <xsl:text>'</xsl:text>
              </xsl:for-each>
              <xsl:text>]);</xsl:text>
            </xsl:attribute>
            <xsl:call-template name="print-text-with-breaks"><xsl:with-param name="text" select="g:testStep/@name" /></xsl:call-template>
          </a>

          <xsl:call-template name="outcome-bar">
            <xsl:with-param name="outcome" select="g:result/g:outcome" />
            <xsl:with-param name="statistics" select="$statistics" />
          </xsl:call-template>
        </span>
        
        <div class="panel">
          <xsl:if test="g:children/g:testStepRun">
            <ul id="summaryPanel-{$id}">
              <xsl:apply-templates select="g:children/g:testStepRun" mode="summary" />
            </ul>
          </xsl:if>
        </div>
      </li>
    </xsl:if>
  </xsl:template>

  <xsl:template match="g:testPackageRun" mode="details">
    <div id="Details" class="section">
      <h2>Details<xsl:if test="$condensed"> (Condensed)</xsl:if></h2>
      <div class="section-content">
        <xsl:choose>
          <xsl:when test="g:testStepRun/g:children/g:testStepRun">
            <xsl:choose>
              <xsl:when test="not($condensed) or g:testStepRun/g:result/g:outcome/@status!='passed'">
                <ul class="testStepRunContainer">
                  <xsl:apply-templates select="g:testStepRun/g:children/g:testStepRun" mode="details" />
                </ul>
              </xsl:when>
              <xsl:otherwise>
                <em>All tests passed.</em>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:when>
          <xsl:otherwise>
            <em>This report does not contain any test runs.</em>
          </xsl:otherwise>
        </xsl:choose>
      </div>
    </div>
  </xsl:template>

  <xsl:template match="g:testStepRun" mode="details">
    <xsl:if test="not($condensed) or g:result/g:outcome/@status!='passed'">
      <xsl:variable name="id" select="g:testStep/@id" />
      
      <xsl:variable name="metadataEntries" select="g:testStep/g:metadata/g:entry" />      
      <xsl:variable name="kind" select="$metadataEntries[@key='TestKind']/g:value" />    
      <xsl:variable name="nestingLevel" select="count(ancestor::g:testStepRun)" />
      
      <xsl:variable name="statisticsRaw">
        <xsl:call-template name="aggregate-statistics">
          <xsl:with-param name="testStepRun" select="." />
        </xsl:call-template>
      </xsl:variable>
      <xsl:variable name="statistics" select="msxsl:node-set($statisticsRaw)/g:statistics" />

      <li id="testStepRun-{$id}">
        <span class="testStepRunHeading testStepRunHeading-Level{$nestingLevel}">
          <xsl:call-template name="toggle">
            <xsl:with-param name="href">detailPanel-<xsl:value-of select="$id"/></xsl:with-param>
          </xsl:call-template>
          
          <xsl:call-template name="icon">
            <xsl:with-param name="kind" select="$kind" />
          </xsl:call-template>

          <xsl:call-template name="code-location-link">
            <xsl:with-param name="path" select="g:testStep/g:codeLocation/@path" />
            <xsl:with-param name="line" select="g:testStep/g:codeLocation/@line" />
            <xsl:with-param name="column" select="g:testStep/g:codeLocation/@column" />
            <xsl:with-param name="content">
              <xsl:call-template name="print-text-with-breaks">
                <xsl:with-param name="text" select="g:testStep/@name" />
              </xsl:call-template>
            </xsl:with-param>
          </xsl:call-template>

          <xsl:call-template name="outcome-bar">
            <xsl:with-param name="outcome" select="g:result/g:outcome" />
            <xsl:with-param name="statistics" select="$statistics" />
            <xsl:with-param name="small" select="not(g:children/g:testStepRun)" />
          </xsl:call-template>
        </span>

        <div id="detailPanel-{$id}" class="panel">
          <xsl:choose>
            <xsl:when test="$nestingLevel = 1">
              <table class="statistics-table">
                <tr class="alternate-row">
                  <td class="statistics-label-cell">Results:</td>
                  <td><xsl:call-template name="format-statistics"><xsl:with-param name="statistics" select="$statistics" /></xsl:call-template></td>
                </tr>
                <tr>
                  <td class="statistics-label-cell">Duration:</td>
                  <td><xsl:value-of select="format-number($statistics/@duration, '0.00')" />s</td>
                </tr>
                <tr class="alternate-row">
                  <td class="statistics-label-cell">Assertions:</td>
                  <td><xsl:value-of select="$statistics/@assertCount" /></td>
                </tr>
              </table>
            </xsl:when>
            <xsl:otherwise>
              <xsl:text>Duration: </xsl:text>
              <xsl:value-of select="format-number($statistics/@duration, '0.00')" />
              <xsl:text>s, Assertions: </xsl:text>
              <xsl:value-of select="$statistics/@assertCount"/>
              <xsl:text>.</xsl:text>
            </xsl:otherwise>
          </xsl:choose>

          <xsl:call-template name="print-metadata-entries">
            <xsl:with-param name="entries" select="$metadataEntries" />
          </xsl:call-template>

          <div id="testStepRun-{g:testStepRun/g:testStep/@id}" class="testStepRun">
            <xsl:apply-templates select="." mode="details-content" />
          </div>

          <xsl:choose>
            <xsl:when test="g:children/g:testStepRun">
              <ul class="testStepRunContainer">
                <xsl:apply-templates select="g:children/g:testStepRun" mode="details" />
              </ul>
            </xsl:when>
            <xsl:when test="g:result/g:outcome/@status = 'passed'">
              <xsl:call-template name="toggle-autoclose">
                <xsl:with-param name="href">detailPanel-<xsl:value-of select="$id"/></xsl:with-param>
              </xsl:call-template>
            </xsl:when>
          </xsl:choose>
        </div>
      </li>
    </xsl:if>
  </xsl:template>

  <xsl:template match="g:testStepRun" mode="details-content">
    <xsl:apply-templates select="g:testLog">
      <xsl:with-param name="stepId" select="g:testStep/@id" />
    </xsl:apply-templates>
  </xsl:template>

  <xsl:template match="g:metadata">
    <xsl:call-template name="print-metadata-entries">
      <xsl:with-param name="entries" select="g:entry" />
    </xsl:call-template>
  </xsl:template>

  <xsl:template name="print-metadata-entries">
    <xsl:param name="entries" />
    <xsl:variable name="visibleEntries" select="$entries[@key != 'TestKind']" />

    <xsl:if test="$visibleEntries">
      <ul class="metadata">
        <xsl:apply-templates select="$visibleEntries">
          <xsl:sort select="translate(@key, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')" lang="en" data-type="text" />
          <xsl:sort select="translate(@value, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')" lang="en" data-type="text" />
        </xsl:apply-templates>
      </ul>
    </xsl:if>
  </xsl:template>

  <xsl:template match="g:entry">
    <li><xsl:value-of select="@key" />: <xsl:call-template name="print-text-with-breaks"><xsl:with-param name="text" select="g:value" /></xsl:call-template></li>
  </xsl:template>

  <xsl:template match="g:testLog">
    <xsl:param name="stepId" />

    <xsl:if test="g:streams/g:stream">
      <div id="log-{$stepId}" class="log">
        <xsl:apply-templates select="g:streams/g:stream" mode="stream">
          <xsl:with-param name="attachments" select="g:attachments" />
        </xsl:apply-templates>
        
        <xsl:if test="g:attachments/g:attachment">
          <div class="logAttachmentList">
            Attachments: <xsl:for-each select="g:attachments/g:attachment">
              <xsl:apply-templates select="." mode="link" /><xsl:if test="position() != last()">, </xsl:if>
            </xsl:for-each>.
          </div>
        </xsl:if>
      </div>
    </xsl:if>
  </xsl:template>

  <xsl:template match="g:streams/g:stream" mode="stream">
    <xsl:param name="attachments" />

    <div class="logStream logStream-{@name}">
      <span class="logStreamHeading">
        <xsl:value-of select="@name" />
      </span>

      <xsl:apply-templates select="g:body" mode="stream">
        <xsl:with-param name="attachments" select="$attachments" />
      </xsl:apply-templates>
    </div>
  </xsl:template>

  <xsl:template match="g:body" mode="stream">
    <xsl:param name="attachments" />

    <div class="logStreamBody">
      <xsl:apply-templates select="g:contents" mode="stream">
        <xsl:with-param name="attachments" select="$attachments" />
      </xsl:apply-templates>
    </div>
  </xsl:template>

  <xsl:template match="g:section" mode="stream">
    <xsl:param name="attachments" />

    <div class="logStreamSection">
      <span class="logStreamSectionHeading">
        <xsl:value-of select="@name"/>
      </span>
      <div>
        <xsl:apply-templates select="g:contents" mode="stream">
          <xsl:with-param name="attachments" select="$attachments" />
        </xsl:apply-templates>
      </div>
    </div>
  </xsl:template>

  <xsl:template match="g:marker" mode="stream">
    <xsl:param name="attachments" />

    <span class="logStreamMarker-{@class}">
      <xsl:choose>
        <xsl:when test="@class = 'CodeLocation'">
          <xsl:call-template name="code-location-link">
            <xsl:with-param name="path" select="g:attributes/g:attribute[@name='path']/@value" />
            <xsl:with-param name="line" select="g:attributes/g:attribute[@name='line']/@value" />
            <xsl:with-param name="column" select="g:attributes/g:attribute[@name='column']/@value" />
            <xsl:with-param name="content">
              <xsl:apply-templates select="g:contents" mode="stream">
                <xsl:with-param name="attachments" select="$attachments" />
              </xsl:apply-templates>
            </xsl:with-param>
          </xsl:call-template>
        </xsl:when>
        <xsl:when test="@class = 'Link'">
          <a class="crossref" href="{g:attributes/g:attribute[@name = 'url']/@value}">
            <xsl:apply-templates select="g:contents" mode="stream">
              <xsl:with-param name="attachments" select="$attachments" />
            </xsl:apply-templates>
          </a>
        </xsl:when>
        <xsl:otherwise>
          <xsl:apply-templates select="g:contents" mode="stream">
            <xsl:with-param name="attachments" select="$attachments" />
          </xsl:apply-templates>
        </xsl:otherwise>
      </xsl:choose>      
    </span>
  </xsl:template>

  <xsl:template match="g:contents" mode="stream">
    <xsl:param name="attachments" />

    <xsl:apply-templates select="child::node()[self::g:text or self::g:section or self::g:embed or self::g:marker]" mode="stream">
      <xsl:with-param name="attachments" select="$attachments" />
    </xsl:apply-templates>
  </xsl:template>

  <xsl:template match="g:text" mode="stream">
    <xsl:param name="attachments" />

    <xsl:call-template name="print-text-with-breaks">
      <xsl:with-param name="text" select="text()" />
    </xsl:call-template>
  </xsl:template>

  <xsl:template match="g:embed" mode="stream">
    <xsl:param name="attachments" />
    <xsl:variable name="attachmentName" select="@attachmentName" />
    
    <xsl:apply-templates select="$attachments/g:attachment[@name=$attachmentName]" mode="embed">
      <xsl:with-param name="id" select="generate-id(.)" />
    </xsl:apply-templates>
  </xsl:template>

  <xsl:template match="g:attachment" mode="link">
    <xsl:choose>
      <xsl:when test="$attachmentBrokerUrl != ''">
        <xsl:variable name="attachmentBrokerQuery"><xsl:value-of select="$attachmentBrokerUrl"/>testStepId=<xsl:value-of select="../../../g:testStep/@id"/>&amp;attachmentName=<xsl:value-of select="@name"/></xsl:variable>
        
        <a href="{$attachmentBrokerQuery}" class="attachmentLink"><xsl:value-of select="@name" /></a>
      </xsl:when>
      <xsl:when test="@contentDisposition = 'link'">
        <xsl:variable name="attachmentUri"><xsl:call-template name="path-to-uri"><xsl:with-param name="path" select="@contentPath" /></xsl:call-template></xsl:variable>
        
        <a href="{$attachmentUri}" class="attachmentLink"><xsl:value-of select="@name" /></a>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="@name" /> (n/a)
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="g:attachment" mode="embed">
    <xsl:param name="id" />
    
    <xsl:choose>
      <!-- When using an attachment broker, we obtain content from a Uri consisting of a query for the attachment data. -->
      <xsl:when test="$attachmentBrokerUrl != ''">
        <xsl:variable name="attachmentBrokerQuery"><xsl:value-of select="$attachmentBrokerUrl"/>testStepId=<xsl:value-of select="../../../g:testStep/@id"/>&amp;attachmentName=<xsl:value-of select="@name"/></xsl:variable>

        <xsl:call-template name="embed-attachment-with-uri">
          <xsl:with-param name="id" select="$id" />
          <xsl:with-param name="name" select="@name" />
          <xsl:with-param name="contentType" select="@contentType" />
          <xsl:with-param name="uri" select="$attachmentBrokerQuery" />
        </xsl:call-template>
      </xsl:when>
      
      <!-- When attachments are linked, we obtain content from a Uri generated from the attachment path. -->
      <xsl:when test="@contentDisposition = 'link'">
        <xsl:variable name="attachmentUri"><xsl:call-template name="path-to-uri"><xsl:with-param name="path" select="@contentPath" /></xsl:call-template></xsl:variable>

        <xsl:call-template name="embed-attachment-with-uri">
          <xsl:with-param name="id" select="$id" />
          <xsl:with-param name="name" select="@name" />
          <xsl:with-param name="contentType" select="@contentType" />
          <xsl:with-param name="uri" select="$attachmentUri" />
        </xsl:call-template>
      </xsl:when>
      
      <!-- When attachments are inline, we try to embed the data within the HTML itself. -->
      <xsl:when test="@contentDisposition = 'inline'">
        <xsl:call-template name="embed-attachment-with-content">
          <xsl:with-param name="id" select="$id" />
          <xsl:with-param name="name" select="@name" />
          <xsl:with-param name="contentType" select="@contentType" />
          <xsl:with-param name="encoding" select="@encoding" />
          <xsl:with-param name="content" select="text()" />
        </xsl:call-template>
      </xsl:when>
      
      <!-- When there is no data, we insert a placeholder. -->
      <xsl:otherwise>
        <xsl:call-template name="embed-attachment-placeholder">
          <xsl:with-param name="id" select="$id" />
          <xsl:with-param name="name" select="@name" />
        </xsl:call-template>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="embed-attachment-with-uri">
    <xsl:param name="id" />
    <xsl:param name="name" />
    <xsl:param name="contentType" />
    <xsl:param name="uri" />

    <xsl:variable name="isImage" select="starts-with($contentType, 'image/')" />
    <xsl:variable name="isHtml" select="starts-with($contentType, 'text/html') or starts-with($contentType, 'text/xhtml')" />
    <xsl:variable name="isText" select="starts-with($contentType, 'text/')" />
    <xsl:variable name="isFlashVideo" select="starts-with($contentType, 'video/x-flv')" />

    <xsl:choose>
      <xsl:when test="$isImage">
        <xsl:call-template name="embed-attachment-image-from-uri">
          <xsl:with-param name="id" select="$id" />
          <xsl:with-param name="name" select="$name" />
          <xsl:with-param name="uri" select="$uri" />
        </xsl:call-template>
      </xsl:when>
      <xsl:when test="$isHtml">
        <xsl:call-template name="embed-attachment-html-from-uri">
          <xsl:with-param name="id" select="$id" />
          <xsl:with-param name="name" select="$name" />
          <xsl:with-param name="uri" select="$uri" />
        </xsl:call-template>
      </xsl:when>
      <xsl:when test="$isText">
        <xsl:call-template name="embed-attachment-text-from-uri">
          <xsl:with-param name="id" select="$id" />
          <xsl:with-param name="name" select="$name" />
          <xsl:with-param name="uri" select="$uri" />
        </xsl:call-template>
      </xsl:when>
      <xsl:when test="$isFlashVideo">
        <xsl:call-template name="embed-attachment-flashvideo-from-uri">
          <xsl:with-param name="id" select="$id" />
          <xsl:with-param name="name" select="$name" />
          <xsl:with-param name="uri" select="$uri" />
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:call-template name="embed-attachment-link">
          <xsl:with-param name="id" select="$id" />
          <xsl:with-param name="name" select="$name" />
          <xsl:with-param name="uri" select="$uri" />
        </xsl:call-template>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="embed-attachment-with-content">
    <xsl:param name="id" />
    <xsl:param name="name" />
    <xsl:param name="contentType" />
    <xsl:param name="encoding" />
    <xsl:param name="content" />

    <xsl:variable name="isImage" select="starts-with($contentType, 'image/')" />
    <xsl:variable name="isHtml" select="starts-with($contentType, 'text/html') or starts-with($contentType, 'text/xhtml')" />
    <xsl:variable name="isText" select="starts-with($contentType, 'text/')" />

    <xsl:choose>
      <xsl:when test="$isImage and $encoding = 'base64'">
        <xsl:call-template name="embed-attachment-image-from-content">
          <xsl:with-param name="id" select="$id" />
          <xsl:with-param name="name" select="$name" />
          <xsl:with-param name="content" select="$content" />
          <xsl:with-param name="contentType" select="$contentType" />
        </xsl:call-template>
      </xsl:when>
      <xsl:when test="$isHtml and $encoding = 'text'">
        <xsl:call-template name="embed-attachment-html-from-content">
          <xsl:with-param name="id" select="$id" />
          <xsl:with-param name="name" select="$name" />
          <xsl:with-param name="content" select="$content" />
        </xsl:call-template>
      </xsl:when>
      <xsl:when test="$isText and $encoding = 'text'">
        <xsl:call-template name="embed-attachment-text-from-content">
          <xsl:with-param name="id" select="$id" />
          <xsl:with-param name="name" select="$name" />
          <xsl:with-param name="content" select="$content" />
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:call-template name="embed-attachment-placeholder">
          <xsl:with-param name="id" select="$id" />
          <xsl:with-param name="name" select="$name" />
        </xsl:call-template>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="embed-attachment-placeholder">
    <xsl:param name="id" />
    <xsl:param name="name" />

    <div id="{$id}" class="logStreamEmbed">
      <xsl:call-template name="embed-attachment-placeholder-text">
        <xsl:with-param name="name" select="$name" />
      </xsl:call-template>
    </div>
  </xsl:template>

  <xsl:template name="embed-attachment-placeholder-text">
    <xsl:param name="name" />

    <xsl:text>Attachment: </xsl:text><xsl:value-of select="$name" /><xsl:text> (n/a)</xsl:text>
  </xsl:template>
  
  <xsl:template name="embed-attachment-link">
    <xsl:param name="id" />
    <xsl:param name="name" />
    <xsl:param name="uri" />

    <div id="{$id}" class="logStreamEmbed">
      <xsl:call-template name="embed-attachment-link-text">
        <xsl:with-param name="name" select="$name" />
        <xsl:with-param name="uri" select="$uri" />
      </xsl:call-template>
    </div>
  </xsl:template>

  <xsl:template name="embed-attachment-link-text">
    <xsl:param name="name" />
    <xsl:param name="uri" />

    <xsl:text>Attachment: </xsl:text><a href="{$uri}" class="attachmentLink"><xsl:value-of select="$name" /></a>
  </xsl:template>
  
  <xsl:template name="embed-attachment-image-from-uri">
    <xsl:param name="id" />
    <xsl:param name="name" />
    <xsl:param name="uri" />

    <div id="{$id}" class="logStreamEmbed">
      <a href="{$uri}" class="attachmentLink">
        <img class="embeddedImage" src="{$uri}" alt="Attachment: {$name}" />
      </a>
    </div>
  </xsl:template>
  
  <xsl:template name="embed-attachment-html-from-uri">
    <xsl:param name="id" />
    <xsl:param name="name" />
    <xsl:param name="uri" />

    <div id="{$id}" class="logStreamEmbed" title="{$name}">
      <xsl:call-template name="embed-attachment-link-text">
        <xsl:with-param name="name" select="$name" />
        <xsl:with-param name="uri" select="$uri" />
      </xsl:call-template>
    </div>
    <script type="text/javascript">setInnerHTMLFromUri('<xsl:value-of select="$id"/>', '<xsl:value-of select="$uri"/>');</script>
  </xsl:template>

  <xsl:template name="embed-attachment-text-from-uri">
    <xsl:param name="id" />
    <xsl:param name="name" />
    <xsl:param name="uri" />

    <div id="{$id}" class="logStreamEmbed" title="{$name}">
      <xsl:call-template name="embed-attachment-link-text">
        <xsl:with-param name="name" select="$name" />
        <xsl:with-param name="uri" select="$uri" />
      </xsl:call-template>
    </div>
    <script type="text/javascript">setPreformattedTextFromUri('<xsl:value-of select="$id"/>', '<xsl:value-of select="$uri"/>');</script>
  </xsl:template>

  <xsl:template name="embed-attachment-flashvideo-from-uri">
    <xsl:param name="id" />
    <xsl:param name="name" />
    <xsl:param name="uri" />

    <div id="{$id}" class="logStreamEmbed" title="{$name}">
      <div id="{$id}-placeholder">
        <xsl:call-template name="embed-attachment-link-text">
          <xsl:with-param name="name" select="$name" />
          <xsl:with-param name="uri" select="$uri" />
        </xsl:call-template>
      </div>
    </div>

    <script type="text/javascript">
      <xsl:text>swfobject.embedSWF('</xsl:text>
      <xsl:value-of select="$jsDir"/>
      <xsl:text>player.swf', '</xsl:text>
      <xsl:value-of select="$id"/>
      <xsl:text>-placeholder', '400', '300', '9.0.98', '</xsl:text>
      <xsl:value-of select="$jsDir"/>
      <xsl:text>expressInstall.swf', {file: </xsl:text>
      <xsl:choose>
        <!-- When using a broker, the uri is absolute. -->
        <xsl:when test="$attachmentBrokerUrl != ''">
          <xsl:text>'</xsl:text>
          <xsl:value-of select="$uri" />
          <xsl:text>'</xsl:text>
        </xsl:when>
        <!-- Otherwise, the uri needs to be made relative to the player location. -->
        <xsl:otherwise>
          <xsl:text>'../../</xsl:text>
          <xsl:value-of select="$uri" />
          <xsl:text>'</xsl:text>
        </xsl:otherwise>
      </xsl:choose>
      <xsl:text>}, {allowfullscreen: 'true', allowscriptaccess: 'always'}, {id: '</xsl:text>
      <xsl:value-of select="$id"/>
      <xsl:text>-player'})</xsl:text>
    </script>
  </xsl:template>

  <xsl:template name="embed-attachment-image-from-content">
    <xsl:param name="id" />
    <xsl:param name="name" />
    <xsl:param name="content" />
    <xsl:param name="contentType" />

    <div id="{$id}" class="logStreamEmbed">
      <img class="embeddedImage" src="data:{$contentType};base64,{$content}" alt="Attachment: {$name}" />
    </div>
  </xsl:template>

  <xsl:template name="embed-attachment-html-from-content">
    <xsl:param name="id" />
    <xsl:param name="name" />
    <xsl:param name="content" />
    
    <div id="{$id}" class="logStreamEmbed" title="{$name}">
      <xsl:call-template name="embed-attachment-placeholder-text">
        <xsl:with-param name="name" select="$name" />
      </xsl:call-template>
      <div id="{$id}-hidden" class="logHiddenData"><xsl:value-of select="$content"/></div>
    </div>
    <script type="text/javascript">setInnerHTMLFromHiddenData('<xsl:value-of select="$id"/>');</script>
  </xsl:template>

  <xsl:template name="embed-attachment-text-from-content">
    <xsl:param name="id" />
    <xsl:param name="name" />
    <xsl:param name="content" />

    <div id="{$id}" class="logStreamEmbed" title="{$name}">
      <xsl:call-template name="embed-attachment-placeholder-text">
        <xsl:with-param name="name" select="$name" />
      </xsl:call-template>
      <div id="{$id}-hidden" class="logHiddenData"><xsl:value-of select="$content"/></div>
    </div>
    <script type="text/javascript">setPreformattedTextFromHiddenData('<xsl:value-of select="$id"/>');</script>
  </xsl:template>

  <xsl:template name="icon">
    <xsl:param name="kind" />

    <xsl:choose>
      <xsl:when test="$kind">
        <span class="testKind testKind-{translate($kind, ' .', '')}" />
      </xsl:when>
      <xsl:otherwise>
        <span class="testKind" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Toggle buttons -->
  <xsl:template name="toggle">
    <xsl:param name="href" />
    
    <img src="{$imgDir}Minus.gif" class="toggle" id="toggle-{$href}" onclick="toggle('{$href}');" alt="Toggle Button" />
  </xsl:template>
  
  <xsl:template name="toggle-stop">
    <img src="{$imgDir}FullStop.gif" class="toggle" alt="Toggle Placeholder" />
  </xsl:template>
  
  <xsl:template name="toggle-autoclose">
    <xsl:param name="href" />
    
    <!-- Auto-close certain toggles by default when JavaScript is available -->
    <script type="text/javascript">toggle('<xsl:value-of select="$href"/>');</script>
  </xsl:template>
  
  <!-- Displays visual statistics using a status bar and outcome icons -->
  <xsl:template name="outcome-bar">
    <xsl:param name="outcome" />
    <xsl:param name="statistics"/>
    <xsl:param name="small" select="0" />

    <table class="outcome-bar">
      <tr>
        <td>
          <div>
            <xsl:attribute name="class">outcome-bar status-<xsl:value-of select="$outcome/@status"/><xsl:if test="$small"> condensed</xsl:if></xsl:attribute>
            <xsl:attribute name="title">
              <xsl:choose>
                <xsl:when test="$outcome/@category"><xsl:value-of select="$outcome/@category"/></xsl:when>
                <xsl:otherwise><xsl:value-of select="$outcome/@status"/></xsl:otherwise>
              </xsl:choose>
            </xsl:attribute>
          </div>
        </td>
      </tr>
    </table>
    
    <xsl:if test="not($small)">
      <span class="outcome-icons">
        <img src="{$imgDir}Passed.gif" alt="Passed"/>
        <xsl:value-of select="$statistics/@passedCount"/>
        <img src="{$imgDir}Failed.gif" alt="Failed"/>
        <xsl:value-of select="$statistics/@failedCount"/>
        <img src="{$imgDir}Ignored.gif" alt="Inconclusive or Skipped"/>
        <xsl:value-of select="$statistics/@inconclusiveCount + $statistics/@skippedCount" />            
      </span>
    </xsl:if>
  </xsl:template>
  
  <xsl:template name="status-from-statistics">
    <xsl:param name="statistics"/>
    
    <xsl:choose>
      <xsl:when test="$statistics/@failedCount > 0">status-failed</xsl:when>
      <xsl:when test="$statistics/@inconclusiveCount > 0">status-inconclusive</xsl:when>
      <xsl:when test="$statistics/@passedCount > 0">status-passed</xsl:when>
      <xsl:otherwise>status-skipped</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="code-location-link">
    <xsl:param name="path" />
    <xsl:param name="line" />
    <xsl:param name="column" />
    <xsl:param name="content" />

    <xsl:choose>
      <xsl:when test="$path and $line > 0 and $column > 0">
        <a class="crossref" href="gallio:navigateTo?path={$path}&amp;line={$line}&amp;column={$column}"><xsl:value-of select="$content"/></a>
      </xsl:when>
      <xsl:when test="$path and $line > 0">
        <a class="crossref" href="gallio:navigateTo?path={$path}&amp;line={$line}"><xsl:value-of select="$content"/></a>
      </xsl:when>
      <xsl:when test="$path">
        <a class="crossref" href="gallio:navigateTo?path={$path}"><xsl:value-of select="$content"/></a>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$content"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="g:logEntries" mode="log">
    <div id="Log" class="section">
      <h2>Diagnostic Log</h2>
      <div class="section-content">
        <ul>
          <xsl:apply-templates select="g:logEntry[@severity != 'debug']" mode="log" />
        </ul>
      </div>
    </div>
  </xsl:template>

  <xsl:template match="g:logEntry" mode="log">
    <li>
      <xsl:attribute name="class">
        logEntry logEntry-severity-<xsl:value-of select="@severity"/>
      </xsl:attribute>
      <div class="logEntry-text">
        <xsl:text>[</xsl:text>
        <xsl:value-of select="@severity"/>
        <xsl:text>] </xsl:text>
        <xsl:call-template name="print-text-with-breaks">
          <xsl:with-param name="text" select="@message" />
        </xsl:call-template>
      </div>

      <xsl:if test="@details">
        <div class="logEntry-details">
          <xsl:text>Details: </xsl:text>
          <xsl:call-template name="print-text-with-breaks">
            <xsl:with-param name="text" select="@details" />
          </xsl:call-template>
        </div>
      </xsl:if>
    </li>
  </xsl:template>

  <!-- Include the common report template -->
  <xsl:include href="Gallio-Report.common.xsl" />  
</xsl:stylesheet>
