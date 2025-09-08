import type { ValidationRequest, ValidationResponse } from './htmlValidationWorker';
import type { Message as HtmlValidateMessage } from "html-validate";

export class HtmlValidationWorkerManager {
  private worker: Worker | null = null;
  private requestId = 0;
  private pendingRequests = new Map<string, {
    resolve: (warnings: HtmlValidateMessage[]) => void;
    reject: (error: Error) => void;
  }>();

  private ensureWorker(): Worker {
    if (!this.worker) {
      this.worker = new Worker(
        new URL('./htmlValidationWorker.ts', import.meta.url),
        { type: 'module' }
      );
      
      this.worker.onmessage = (event: MessageEvent<ValidationResponse>) => {
        const { requestId, warnings, error } = event.data;
        const pending = this.pendingRequests.get(requestId);
        
        if (pending) {
          this.pendingRequests.delete(requestId);
          
          if (error) {
            pending.reject(new Error(error));
          } else {
            pending.resolve(warnings);
          }
        }
      };
      
      this.worker.onerror = (error) => {
        // Reject all pending requests
        for (const pending of this.pendingRequests.values()) {
          pending.reject(new Error(`Worker error: ${error.message}`));
        }
        this.pendingRequests.clear();
      };
    }
    
    return this.worker;
  }

  async validateHtml(html: string, config: any): Promise<HtmlValidateMessage[]> {
    const worker = this.ensureWorker();
    const requestId = `req_${++this.requestId}`;
    
    return new Promise<HtmlValidateMessage[]>((resolve, reject) => {
      this.pendingRequests.set(requestId, { resolve, reject });
      
      const request: ValidationRequest = {
        html,
        config,
        requestId
      };
      
      worker.postMessage(request);
    });
  }

  destroy(): void {
    if (this.worker) {
      this.worker.terminate();
      this.worker = null;
    }
    
    // Reject all pending requests
    for (const pending of this.pendingRequests.values()) {
      pending.reject(new Error('Worker terminated'));
    }
    this.pendingRequests.clear();
  }
}