import type { CompatibilityRequest, CompatibilityResponse, CompatibilityWarning } from './htmlCompatibilityWorker';

export type { CompatibilityWarning };

export class HtmlCompatibilityWorkerManager {
  private worker: Worker | null = null;
  private requestId = 0;
  private pendingRequests = new Map<string, {
    resolve: (warnings: CompatibilityWarning[]) => void;
    reject: (error: Error) => void;
  }>();

  private ensureWorker(): Worker {
    if (!this.worker) {
      this.worker = new Worker(
        new URL('./htmlCompatibilityWorker.ts', import.meta.url),
        { type: 'module' }
      );
      
      this.worker.onmessage = (event: MessageEvent<CompatibilityResponse>) => {
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

  async checkCompatibility(html: string): Promise<CompatibilityWarning[]> {
    const worker = this.ensureWorker();
    const requestId = `req_${++this.requestId}`;
    
    return new Promise<CompatibilityWarning[]>((resolve, reject) => {
      this.pendingRequests.set(requestId, { resolve, reject });
      
      const request: CompatibilityRequest = {
        html,
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