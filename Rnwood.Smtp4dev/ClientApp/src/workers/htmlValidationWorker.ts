import { HtmlValidate, Message as HtmlValidateMessage } from "html-validate";

export interface ValidationRequest {
  html: string;
  config: any;
  requestId: string;
}

export interface ValidationResponse {
  requestId: string;
  warnings: HtmlValidateMessage[];
  error?: string;
}

// Handle messages from the main thread
self.onmessage = async (event: MessageEvent<ValidationRequest>) => {
  const { html, config, requestId } = event.data;
  
  try {
    const validator = new HtmlValidate(config);
    const report = await validator.validateString(html, "messagebody");
    
    const warnings: HtmlValidateMessage[] = [];
    for (const r of report.results) {
      warnings.push(...r.messages);
    }
    
    const response: ValidationResponse = {
      requestId,
      warnings
    };
    
    self.postMessage(response);
  } catch (error) {
    const response: ValidationResponse = {
      requestId,
      warnings: [],
      error: error instanceof Error ? error.message : String(error)
    };
    
    self.postMessage(response);
  }
};