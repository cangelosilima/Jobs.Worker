# Job Scheduler Admin UI

A modern, real-time admin interface for the Job Scheduler Worker system, built with React 18, TypeScript, and Material UI.

## Features

- 📊 **Real-time Dashboard** with metrics, charts, and live updates via SignalR
- ▶️ **Running Jobs Monitor** with live status updates and execution tracking
- 📜 **Job History** with advanced filtering and pagination
- 📅 **Schedule Management** with multi-step wizard for creating complex schedules
- 🔍 **Job Details** with comprehensive information display
- 📝 **Audit Logs** with real-time streaming
- 🎨 **Light/Dark Theme** support
- 🔐 **Role-based Access Control** (Admin, Operator, Viewer, Job Owner)
- 🔔 **Real-time Notifications** via SignalR

## Tech Stack

- **React 18** - Modern React with hooks
- **TypeScript** - Type-safe development
- **Vite** - Fast build tool with HMR
- **Material UI 6** - Beautiful and accessible UI components
- **TanStack Query** - Powerful server state management
- **Zustand** - Lightweight client state management
- **SignalR** - Real-time bidirectional communication
- **Recharts** - Beautiful charts and graphs
- **React Router v6+** - Client-side routing
- **Day.js** - Date manipulation and formatting
- **Axios** - HTTP client with interceptors

## Prerequisites

- Node.js 18+ and npm/yarn
- Backend API running on `http://localhost:5000`
- SignalR hub available at `/hubs/jobs`

## Installation

```bash
# Navigate to frontend directory
cd frontend

# Install dependencies
npm install

# or with yarn
yarn install
```

## Development

```bash
# Start development server (runs on http://localhost:3000)
npm run dev

# or with yarn
yarn dev
```

The dev server includes:
- Hot Module Replacement (HMR)
- TypeScript type checking
- API proxy to `http://localhost:5000`
- SignalR WebSocket proxy

## Build for Production

```bash
# Build optimized production bundle
npm run build

# Preview production build locally
npm run preview

# Type check without building
npm run type-check
```

## Project Structure

```
frontend/
├── src/
│   ├── api/                    # API client and types
│   │   ├── client.ts          # Axios configuration with interceptors
│   │   ├── types.ts           # TypeScript types matching backend models
│   │   └── jobs.api.ts        # API service methods
│   │
│   ├── components/            # Reusable components
│   │   ├── common/            # Common UI components
│   │   └── layout/            # Layout components
│   │
│   ├── hooks/                 # Custom React hooks
│   │
│   ├── layouts/               # Application layouts
│   │   └── MainLayout.tsx     # Main app layout with sidebar
│   │
│   ├── modules/               # Feature modules
│   │   ├── dashboard/         # Dashboard with metrics and charts
│   │   ├── jobs/
│   │   │   ├── running/       # Real-time running jobs monitor
│   │   │   ├── history/       # Job execution history
│   │   │   └── details/       # Job details view
│   │   ├── schedules/
│   │   │   ├── list/          # Schedule list view
│   │   │   ├── create/        # Multi-step schedule wizard
│   │   │   └── edit/          # Schedule editor
│   │   ├── dependencies/      # Job dependencies (DAG)
│   │   ├── audit/             # Audit logs with real-time stream
│   │   └── notifications/     # Notification center
│   │
│   ├── routing/               # Routes and navigation
│   │   ├── ProtectedRoute.tsx # Route protection with RBAC
│   │   └── routes.tsx         # Route definitions
│   │
│   ├── services/              # Services
│   │   └── signalr.ts         # SignalR service with auto-reconnect
│   │
│   ├── state/                 # Global state management
│   │   ├── auth.store.ts      # Authentication and RBAC
│   │   ├── ui.store.ts        # UI preferences (theme, sidebar)
│   │   └── notification.store.ts # Notifications
│   │
│   ├── theme/                 # Theming
│   │   └── theme.ts           # Material UI theme config
│   │
│   ├── utils/                 # Utility functions
│   │
│   ├── App.tsx                # Main app component
│   ├── main.tsx               # Entry point
│   └── index.css              # Global styles
│
├── public/                    # Static assets
├── index.html                 # HTML template
├── package.json               # Dependencies and scripts
├── tsconfig.json              # TypeScript configuration
├── vite.config.ts             # Vite configuration
└── .gitignore                 # Git ignore rules
```

## Configuration

### API Endpoint

The frontend is configured to connect to the backend API via Vite's proxy (see `vite.config.ts`):

```typescript
server: {
  port: 3000,
  proxy: {
    '/api': {
      target: 'http://localhost:5000',
    },
    '/hubs': {
      target: 'http://localhost:5000',
      ws: true,  // WebSocket support for SignalR
    },
  },
}
```

To change the backend URL, edit `vite.config.ts`.

### Environment Variables

Create a `.env` file in the `frontend/` directory if needed:

```env
VITE_API_BASE_URL=http://localhost:5000
```

## Features Deep Dive

### Real-time Updates with SignalR

The application uses SignalR for real-time bidirectional communication:

- **Job Execution Updates**: Live status changes as jobs execute
- **Metrics Updates**: Dashboard metrics refresh automatically
- **Audit Log Streaming**: New audit entries appear in real-time
- **Notifications**: Push notifications for job events

SignalR service includes:
- Automatic reconnection with exponential backoff
- Connection state monitoring
- Event subscription/unsubscription
- Type-safe event handlers

### State Management

#### TanStack Query (Server State)
- Automatic caching and invalidation
- Background refetching
- Optimistic updates
- Pagination support

#### Zustand (Client State)
- **Auth Store**: User authentication, roles, permissions
- **UI Store**: Theme mode, sidebar state, drawer state
- **Notification Store**: In-app notifications with unread count

### Role-Based Access Control

Four roles supported:
- **Admin**: Full access to all features
- **Operator**: Can trigger jobs, manage schedules, view all data
- **Viewer**: Read-only access
- **Job Owner**: Can manage owned jobs only

Protected routes automatically redirect unauthorized users.

### Theme Support

Toggle between light and dark themes:
- Persistent theme preference (stored in localStorage)
- Material UI with custom color palettes
- Consistent typography and spacing

## API Integration

The frontend communicates with the backend via REST APIs and SignalR:

### REST Endpoints

```typescript
// Dashboard
GET /api/dashboard/stats
GET /api/dashboard/execution-trends?days=7
GET /api/dashboard/top-failing-jobs?limit=10
GET /api/dashboard/stale-jobs?daysThreshold=7
GET /api/dashboard/upcoming-schedules?hoursAhead=24

// Jobs
GET /api/jobs
GET /api/jobs/{id}
POST /api/jobs
PUT /api/jobs/{id}
DELETE /api/jobs/{id}
POST /api/jobs/{id}/trigger
POST /api/jobs/{id}/activate
POST /api/jobs/{id}/disable
GET /api/jobs/{id}/schedules
GET /api/jobs/{id}/executions

// Executions
GET /api/executions
GET /api/executions/{id}
GET /api/executions/running
POST /api/executions/{id}/cancel
GET /api/executions/{id}/logs

// Schedules
POST /api/jobs/{jobId}/schedules

// Audit Logs
GET /api/audit
GET /api/audit/{id}
GET /api/audit/{entityType}/{entityId}
```

### SignalR Events

```typescript
// Subscribe to events
signalRService.on('JobExecutionUpdated', (data) => { ... });
signalRService.on('MetricsUpdated', (data) => { ... });
signalRService.on('AuditLogAdded', (data) => { ... });
signalRService.on('NotificationReceived', (data) => { ... });
```

## Development Workflow

1. **Start Backend API**: Ensure the backend is running on port 5000
2. **Start Frontend**: `npm run dev`
3. **Open Browser**: Navigate to `http://localhost:3000`
4. **Login**: Use credentials (implementation pending)
5. **Monitor**: Check browser console for errors and SignalR connection status

## Common Issues

### SignalR Connection Failed

- Ensure backend API is running
- Check if SignalR hub is configured at `/hubs/jobs`
- Verify CORS settings allow `http://localhost:3000`

### API 404 Errors

- Verify all API endpoints exist in backend
- Check backend routing configuration
- Ensure proxy configuration in `vite.config.ts` is correct

### Build Errors

- Run `npm run type-check` to identify TypeScript errors
- Ensure all dependencies are installed
- Clear `node_modules` and reinstall if needed

## Testing

```bash
# Run tests (when implemented)
npm test

# Run tests with coverage
npm run test:coverage
```

## Contributing

1. Follow TypeScript best practices
2. Use functional components with hooks
3. Keep components small and focused
4. Add proper TypeScript types
5. Test real-time features with SignalR
6. Ensure responsive design

## License

Proprietary - All rights reserved

## Support

For issues and questions, please contact the development team.
