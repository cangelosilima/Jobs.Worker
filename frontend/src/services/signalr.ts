import * as signalR from '@microsoft/signalr';

export interface JobExecutionUpdate {
  executionId: string;
  jobId: string;
  jobName: string;
  status: string;
  startTime?: string;
  endTime?: string;
  output?: string;
  errorMessage?: string;
  progress?: number;
}

export interface MetricsUpdate {
  totalJobs: number;
  activeJobs: number;
  runningExecutions: number;
  failedToday: number;
  succeededToday: number;
  successRatePercentage: number;
}

export interface AuditLogEntry {
  id: string;
  timestamp: string;
  userId: string;
  userName: string;
  action: string;
  entityType: string;
  entityId: string;
  changes: string;
}

export interface NotificationMessage {
  id: string;
  timestamp: string;
  severity: 'info' | 'warning' | 'error' | 'success';
  title: string;
  message: string;
  jobId?: string;
  executionId?: string;
}

type EventHandler<T> = (data: T) => void;

class SignalRService {
  private hubConnection: signalR.HubConnection | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 2000;
  private eventHandlers: Map<string, Set<EventHandler<any>>> = new Map();

  constructor() {
    this.initializeConnection();
  }

  private initializeConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/jobs', {
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
            console.error('Maximum reconnection attempts reached');
            return null;
          }
          const delay = Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
          return delay;
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();
    this.setupConnectionHandlers();
  }

  private setupConnectionHandlers(): void {
    if (!this.hubConnection) return;

    this.hubConnection.onclose((error) => {
      console.error('SignalR connection closed:', error);
      this.attemptReconnect();
    });

    this.hubConnection.onreconnecting((error) => {
      console.warn('SignalR reconnecting:', error);
    });

    this.hubConnection.onreconnected((connectionId) => {
      console.info('SignalR reconnected:', connectionId);
      this.reconnectAttempts = 0;
    });
  }

  private setupEventHandlers(): void {
    if (!this.hubConnection) return;

    // Job execution updates
    this.hubConnection.on('JobExecutionUpdated', (data: JobExecutionUpdate) => {
      this.emit('JobExecutionUpdated', data);
    });

    // Metrics updates
    this.hubConnection.on('MetricsUpdated', (data: MetricsUpdate) => {
      this.emit('MetricsUpdated', data);
    });

    // Audit log entries
    this.hubConnection.on('AuditLogAdded', (data: AuditLogEntry) => {
      this.emit('AuditLogAdded', data);
    });

    // Notifications
    this.hubConnection.on('NotificationReceived', (data: NotificationMessage) => {
      this.emit('NotificationReceived', data);
    });

    // Job started
    this.hubConnection.on('JobStarted', (data: JobExecutionUpdate) => {
      this.emit('JobStarted', data);
    });

    // Job completed
    this.hubConnection.on('JobCompleted', (data: JobExecutionUpdate) => {
      this.emit('JobCompleted', data);
    });

    // Job failed
    this.hubConnection.on('JobFailed', (data: JobExecutionUpdate) => {
      this.emit('JobFailed', data);
    });
  }

  public async start(): Promise<void> {
    if (!this.hubConnection) {
      this.initializeConnection();
    }

    if (this.hubConnection?.state === signalR.HubConnectionState.Disconnected) {
      try {
        await this.hubConnection.start();
        console.info('SignalR connected successfully');
        this.reconnectAttempts = 0;
      } catch (error) {
        console.error('Error starting SignalR connection:', error);
        this.attemptReconnect();
      }
    }
  }

  public async stop(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      console.info('SignalR connection stopped');
    }
  }

  private async attemptReconnect(): Promise<void> {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.error('Maximum reconnection attempts reached');
      return;
    }

    this.reconnectAttempts++;
    const delay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1);

    console.info(`Attempting to reconnect in ${delay}ms (attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts})`);

    setTimeout(async () => {
      try {
        await this.start();
      } catch (error) {
        console.error('Reconnection attempt failed:', error);
      }
    }, delay);
  }

  public on<T>(eventName: string, handler: EventHandler<T>): void {
    if (!this.eventHandlers.has(eventName)) {
      this.eventHandlers.set(eventName, new Set());
    }
    this.eventHandlers.get(eventName)!.add(handler);
  }

  public off<T>(eventName: string, handler: EventHandler<T>): void {
    const handlers = this.eventHandlers.get(eventName);
    if (handlers) {
      handlers.delete(handler);
    }
  }

  private emit<T>(eventName: string, data: T): void {
    const handlers = this.eventHandlers.get(eventName);
    if (handlers) {
      handlers.forEach((handler) => handler(data));
    }
  }

  public async invoke<T>(methodName: string, ...args: any[]): Promise<T> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('SignalR connection is not established');
    }
    return await this.hubConnection.invoke<T>(methodName, ...args);
  }

  public isConnected(): boolean {
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }

  public getConnectionState(): signalR.HubConnectionState | null {
    return this.hubConnection?.state ?? null;
  }
}

// Export singleton instance
export const signalRService = new SignalRService();
