import * as signalR from '@microsoft/signalr';
import { defaultSettings, ClientSettings } from './api-base';

/**
 * SignalR DTOs
 */
export interface JobExecutionUpdateDto {
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

export interface MetricsUpdateDto {
    totalJobs: number;
    activeJobs: number;
    runningExecutions: number;
    failedToday: number;
    succeededToday: number;
    successRatePercentage: number;
}

export interface AuditLogEntryDto {
    id: string;
    timestamp: string;
    userId: string;
    userName: string;
    action: string;
    entityType: string;
    entityId: string;
    changes: string;
}

export interface NotificationDto {
    id: string;
    timestamp: string;
    severity: string;
    title: string;
    message: string;
    jobId?: string;
    executionId?: string;
}

/**
 * SignalR Hub Client for Jobs.Worker
 */
export class JobsHubClient {
    private connection: signalR.HubConnection;
    private settings: ClientSettings;

    // Event handlers
    public onJobExecutionUpdated?: (update: JobExecutionUpdateDto) => void;
    public onJobStarted?: (update: JobExecutionUpdateDto) => void;
    public onJobCompleted?: (update: JobExecutionUpdateDto) => void;
    public onJobFailed?: (update: JobExecutionUpdateDto) => void;
    public onMetricsUpdated?: (metrics: MetricsUpdateDto) => void;
    public onAuditLogAdded?: (auditLog: AuditLogEntryDto) => void;
    public onNotificationReceived?: (notification: NotificationDto) => void;

    // Connection state events
    public onReconnecting?: (error?: Error) => void;
    public onReconnected?: (connectionId?: string) => void;
    public onClosed?: (error?: Error) => void;

    constructor(settings?: Partial<ClientSettings>) {
        this.settings = { ...defaultSettings, ...settings };
        const hubUrl = `${this.settings.baseUrl.replace(/\/$/, '')}/hubs/jobs`;

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect([0, 2000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.registerConnectionHandlers();
    }

    /**
     * Register connection lifecycle handlers
     */
    private registerConnectionHandlers(): void {
        this.connection.onreconnecting((error) => {
            console.log('SignalR reconnecting...', error);
            if (this.onReconnecting) {
                this.onReconnecting(error);
            }
        });

        this.connection.onreconnected((connectionId) => {
            console.log('SignalR reconnected:', connectionId);
            if (this.onReconnected) {
                this.onReconnected(connectionId);
            }
        });

        this.connection.onclose((error) => {
            console.log('SignalR connection closed', error);
            if (this.onClosed) {
                this.onClosed(error);
            }
        });
    }

    /**
     * Subscribe to all server events
     */
    public subscribeToEvents(): void {
        this.connection.on('JobExecutionUpdated', (update: JobExecutionUpdateDto) => {
            if (this.onJobExecutionUpdated) {
                this.onJobExecutionUpdated(update);
            }
        });

        this.connection.on('JobStarted', (update: JobExecutionUpdateDto) => {
            if (this.onJobStarted) {
                this.onJobStarted(update);
            }
        });

        this.connection.on('JobCompleted', (update: JobExecutionUpdateDto) => {
            if (this.onJobCompleted) {
                this.onJobCompleted(update);
            }
        });

        this.connection.on('JobFailed', (update: JobExecutionUpdateDto) => {
            if (this.onJobFailed) {
                this.onJobFailed(update);
            }
        });

        this.connection.on('MetricsUpdated', (metrics: MetricsUpdateDto) => {
            if (this.onMetricsUpdated) {
                this.onMetricsUpdated(metrics);
            }
        });

        this.connection.on('AuditLogAdded', (auditLog: AuditLogEntryDto) => {
            if (this.onAuditLogAdded) {
                this.onAuditLogAdded(auditLog);
            }
        });

        this.connection.on('NotificationReceived', (notification: NotificationDto) => {
            if (this.onNotificationReceived) {
                this.onNotificationReceived(notification);
            }
        });
    }

    /**
     * Start the SignalR connection
     */
    public async start(): Promise<void> {
        if (this.connection.state === signalR.HubConnectionState.Disconnected) {
            await this.connection.start();
            console.log('SignalR connected');
        }
    }

    /**
     * Stop the SignalR connection
     */
    public async stop(): Promise<void> {
        if (this.connection.state !== signalR.HubConnectionState.Disconnected) {
            await this.connection.stop();
            console.log('SignalR disconnected');
        }
    }

    /**
     * Get the current connection state
     */
    public get state(): signalR.HubConnectionState {
        return this.connection.state;
    }

    // Client-to-Server methods
    public async sendJobExecutionUpdate(update: JobExecutionUpdateDto): Promise<void> {
        await this.connection.invoke('SendJobExecutionUpdate', update);
    }

    public async sendJobStarted(update: JobExecutionUpdateDto): Promise<void> {
        await this.connection.invoke('SendJobStarted', update);
    }

    public async sendJobCompleted(update: JobExecutionUpdateDto): Promise<void> {
        await this.connection.invoke('SendJobCompleted', update);
    }

    public async sendJobFailed(update: JobExecutionUpdateDto): Promise<void> {
        await this.connection.invoke('SendJobFailed', update);
    }

    public async sendMetricsUpdate(metrics: MetricsUpdateDto): Promise<void> {
        await this.connection.invoke('SendMetricsUpdate', metrics);
    }

    public async sendAuditLogEntry(auditLog: AuditLogEntryDto): Promise<void> {
        await this.connection.invoke('SendAuditLogEntry', auditLog);
    }

    public async sendNotification(notification: NotificationDto): Promise<void> {
        await this.connection.invoke('SendNotification', notification);
    }
}
