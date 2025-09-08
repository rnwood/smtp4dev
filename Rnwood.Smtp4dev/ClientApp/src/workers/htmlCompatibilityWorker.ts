import { doIUseEmail } from '@jsx-email/doiuse-email';

export interface CompatibilityRequest {
  html: string;
  requestId: string;
}

export interface CompatibilityResponse {
  requestId: string;
  warnings: CompatibilityWarning[];
  error?: string;
}

export interface CompatibilityWarning {
  message: string;
  feature: string;
  type: string;
  browsers: string[];
  url: string;
  isError: boolean;
}

function parseWarning(warning: string, isError: boolean): CompatibilityWarning {
  const details = { message: warning, type: "", feature: "", browser: "", url: "", isError: false };
  const detailsMatch = warning.match(/^`(.+)` (support )?is (.+) (by|for) `(.+)`$/);

  if (detailsMatch) {
    details.feature = detailsMatch[1] ?? "";
    details.type = detailsMatch[3] ?? "";
    details.browser = detailsMatch[5] ?? "";
    details.isError = isError;

    if (details.feature.endsWith(" element")) {
      details.url = `https://www.caniemail.com/features/html-${details.feature.replace("<", "").replace("> element", "")}/`;
    } else {
      details.url = `https://www.caniemail.com/features/css-${details.feature.replace(":", "-")}/`;
    }
  } else {
    details.type = warning;
  }

  return {
    message: details.message,
    feature: details.feature,
    type: details.type,
    browsers: [details.browser],
    url: details.url,
    isError: details.isError
  };
}

// Handle messages from the main thread
self.onmessage = async (event: MessageEvent<CompatibilityRequest>) => {
  const { html, requestId } = event.data;
  
  try {
    const doIUseResults = doIUseEmail(html, { emailClients: ["*"] });
    
    const allWarnings = [];
    for (const warning of doIUseResults.warnings) {
      const details = parseWarning(warning, false);
      allWarnings.push(details);
    }

    if (doIUseResults.success === false) {
      for (const warning of doIUseResults.errors) {
        const details = parseWarning(warning, true);
        allWarnings.push(details);
      }
    }

    // Group by feature and type
    const allGrouped = Object.groupBy(allWarnings, i => i.feature + " " + i.type);
    const newWarnings: CompatibilityWarning[] = [];
    
    for (const groupKey in allGrouped) {
      const groupItems = allGrouped[groupKey]!;
      newWarnings.push({
        type: groupItems[0].type,
        feature: groupItems[0].feature,
        message: groupItems[0].message,
        url: groupItems[0].url,
        browsers: groupItems.map(i => i.browsers[0]).filter((value, index, array) => array.indexOf(value) === index),
        isError: groupItems[0].isError
      });
    }
    
    const response: CompatibilityResponse = {
      requestId,
      warnings: newWarnings
    };
    
    self.postMessage(response);
  } catch (error) {
    const response: CompatibilityResponse = {
      requestId,
      warnings: [],
      error: error instanceof Error ? error.message : String(error)
    };
    
    self.postMessage(response);
  }
};