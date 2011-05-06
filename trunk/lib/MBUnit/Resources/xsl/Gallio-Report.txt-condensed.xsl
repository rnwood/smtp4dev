<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                xmlns:g="http://www.gallio.org/">
  
  <xsl:param name="show-passed-tests" select="false()" />
  <xsl:param name="show-failed-tests" select="true()" />
  <xsl:param name="show-inconclusive-tests" select="true()" />
  <xsl:param name="show-skipped-tests" select="true()" />

  <xsl:include href="Gallio-Report.txt-common.xsl" />
</xsl:stylesheet>