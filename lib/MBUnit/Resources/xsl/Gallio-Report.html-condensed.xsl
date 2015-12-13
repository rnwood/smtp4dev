<?xml version="1.0" encoding="iso-8859-1"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:g="http://www.gallio.org/">
  <xsl:output method="html" doctype-system="http://www.w3.org/TR/html4/strict.dtd"
              doctype-public="-//W3C//DTD HTML 4.01//EN" indent="no" encoding="utf-8" omit-xml-declaration="yes" />
  <xsl:param name="resourceRoot" select="''" />

  <xsl:variable name="cssDir"><xsl:if test="$resourceRoot != ''"><xsl:value-of select="$resourceRoot"/>/</xsl:if>css/</xsl:variable>
  <xsl:variable name="jsDir"><xsl:if test="$resourceRoot != ''"><xsl:value-of select="$resourceRoot"/>/</xsl:if>js/</xsl:variable>
  <xsl:variable name="imgDir"><xsl:if test="$resourceRoot != ''"><xsl:value-of select="$resourceRoot"/>/</xsl:if>img/</xsl:variable>
  <xsl:variable name="attachmentBrokerUrl"></xsl:variable>
  <xsl:variable name="condensed" select="1" />

  <xsl:template match="/">
    <xsl:apply-templates select="/g:report" mode="html-document" />
  </xsl:template>
  
  <!-- Include the base HTML / XHTML report template -->
  <xsl:include href="Gallio-Report.html+xhtml.xsl" />
</xsl:stylesheet>
