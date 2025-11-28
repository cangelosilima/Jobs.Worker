import axios, { AxiosInstance, AxiosError, InternalAxiosRequestConfig } from 'axios';

export interface ApiError {
  message: string;
  statusCode: number;
  details?: any;
}

class ApiClient {
  private instance: AxiosInstance;

  constructor() {
    this.instance = axios.create({
      baseURL: '/api',
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors(): void {
    // Request interceptor
    this.instance.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        // Add correlation ID for tracing
        const correlationId = crypto.randomUUID();
        config.headers['X-Correlation-ID'] = correlationId;

        // Log request in development
        if (import.meta.env.DEV) {
          console.log(`[API Request] ${config.method?.toUpperCase()} ${config.url}`, {
            correlationId,
            data: config.data,
            params: config.params,
          });
        }

        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // Response interceptor
    this.instance.interceptors.response.use(
      (response) => {
        // Log response in development
        if (import.meta.env.DEV) {
          console.log(`[API Response] ${response.config.method?.toUpperCase()} ${response.config.url}`, {
            status: response.status,
            data: response.data,
          });
        }
        return response;
      },
      (error: AxiosError) => {
        const apiError = this.handleError(error);

        // Log error
        console.error('[API Error]', {
          url: error.config?.url,
          method: error.config?.method,
          status: error.response?.status,
          message: apiError.message,
          details: apiError.details,
        });

        return Promise.reject(apiError);
      }
    );
  }

  private handleError(error: AxiosError): ApiError {
    if (error.response) {
      // Server responded with error status
      const status = error.response.status;
      const data = error.response.data as any;

      return {
        message: data?.message || data?.title || this.getStatusMessage(status),
        statusCode: status,
        details: data?.errors || data?.detail || data,
      };
    } else if (error.request) {
      // Request made but no response received
      return {
        message: 'No response from server. Please check your network connection.',
        statusCode: 0,
      };
    } else {
      // Error setting up the request
      return {
        message: error.message || 'An unexpected error occurred',
        statusCode: 0,
      };
    }
  }

  private getStatusMessage(status: number): string {
    switch (status) {
      case 400:
        return 'Bad request. Please check your input.';
      case 401:
        return 'Unauthorized. Please login again.';
      case 403:
        return 'Forbidden. You do not have permission to perform this action.';
      case 404:
        return 'Resource not found.';
      case 409:
        return 'Conflict. The resource already exists or cannot be modified.';
      case 422:
        return 'Validation error. Please check your input.';
      case 500:
        return 'Internal server error. Please try again later.';
      case 503:
        return 'Service unavailable. Please try again later.';
      default:
        return `Request failed with status ${status}`;
    }
  }

  public get<T>(url: string, params?: any): Promise<T> {
    return this.instance.get<T>(url, { params }).then((response) => response.data);
  }

  public post<T>(url: string, data?: any): Promise<T> {
    return this.instance.post<T>(url, data).then((response) => response.data);
  }

  public put<T>(url: string, data?: any): Promise<T> {
    return this.instance.put<T>(url, data).then((response) => response.data);
  }

  public patch<T>(url: string, data?: any): Promise<T> {
    return this.instance.patch<T>(url, data).then((response) => response.data);
  }

  public delete<T>(url: string): Promise<T> {
    return this.instance.delete<T>(url).then((response) => response.data);
  }

  public getInstance(): AxiosInstance {
    return this.instance;
  }
}

export const apiClient = new ApiClient();
