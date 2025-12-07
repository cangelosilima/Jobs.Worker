/**
 * Base API client configuration and utilities
 */

export interface ClientSettings {
    baseUrl: string;
    healthCheckUrl: string;
    retryCount: number;
    timeoutSeconds: number;
}

export const defaultSettings: ClientSettings = {
    baseUrl: 'https://localhost:5001',
    healthCheckUrl: 'https://localhost:5001/health',
    retryCount: 3,
    timeoutSeconds: 30
};

export class BaseApiClient {
    protected baseUrl: string;
    protected healthCheckUrl: string;
    protected retryCount: number;
    protected timeout: number;

    constructor(settings?: Partial<ClientSettings>) {
        const config = { ...defaultSettings, ...settings };
        this.baseUrl = config.baseUrl;
        this.healthCheckUrl = config.healthCheckUrl;
        this.retryCount = config.retryCount;
        this.timeout = config.timeoutSeconds * 1000;
    }

    /**
     * Performs a health check against the API
     */
    async healthCheck(): Promise<boolean> {
        try {
            const response = await fetch(this.healthCheckUrl, {
                method: 'GET',
                signal: AbortSignal.timeout(this.timeout)
            });
            return response.ok;
        } catch (error) {
            console.error('Health check failed:', error);
            return false;
        }
    }

    /**
     * Executes a fetch request with retry logic
     */
    protected async fetchWithRetry<T>(
        url: string,
        options: RequestInit = {}
    ): Promise<T> {
        let lastError: Error | null = null;

        for (let attempt = 0; attempt <= this.retryCount; attempt++) {
            try {
                const response = await fetch(url, {
                    ...options,
                    signal: AbortSignal.timeout(this.timeout)
                });

                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }

                return await response.json();
            } catch (error) {
                lastError = error as Error;

                if (attempt < this.retryCount) {
                    const delay = Math.pow(2, attempt) * 1000;
                    console.log(`Request failed. Waiting ${delay}ms before retry #${attempt + 1}`);
                    await this.sleep(delay);
                }
            }
        }

        throw lastError || new Error('Request failed after retries');
    }

    /**
     * Sleep utility for retry logic
     */
    private sleep(ms: number): Promise<void> {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
}
