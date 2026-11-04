import { Suspense, lazy, type ReactNode } from 'react'
import { Route, Routes, Navigate } from 'react-router-dom'
import { useAuth } from './hooks/useAuth'
import { AppLayout, RequirePermission, RequirePlatformAdmin } from './components/layouts/AppLayout'
import { LoginPage } from './pages/LoginPage'

const SuperAdminDashboardPage = lazy(() =>
  import('./pages/superadmin/DashboardPage').then((m) => ({ default: m.SuperAdminDashboardPage }))
)
const OrganizationsPage = lazy(() =>
  import('./pages/superadmin/OrganizationsPage').then((m) => ({ default: m.OrganizationsPage }))
)
const OrganizationDetailPage = lazy(() =>
  import('./pages/superadmin/OrganizationDetailPage').then((m) => ({ default: m.OrganizationDetailPage }))
)
const SettingsPage = lazy(() =>
  import('./pages/superadmin/SettingsPage').then((m) => ({ default: m.SettingsPage }))
)
const DashboardHomePage = lazy(() =>
  import('./pages/DashboardHomePage').then((m) => ({ default: m.DashboardHomePage }))
)
const CustomersPage = lazy(() =>
  import('./pages/CustomersPage').then((m) => ({ default: m.CustomersPage }))
)
const CustomerDetailPage = lazy(() =>
  import('./pages/CustomerDetailPage').then((m) => ({ default: m.CustomerDetailPage }))
)
const CreateCustomerPage = lazy(() =>
  import('./pages/CreateCustomerPage').then((m) => ({ default: m.CreateCustomerPage }))
)
const LoansPage = lazy(() =>
  import('./pages/LoansPage').then((m) => ({ default: m.LoansPage }))
)
const LoanDetailPage = lazy(() =>
  import('./pages/LoanDetailPage').then((m) => ({ default: m.LoanDetailPage }))
)
const CreateLoanPage = lazy(() =>
  import('./pages/CreateLoanPage').then((m) => ({ default: m.CreateLoanPage }))
)
const LoanProductsPage = lazy(() =>
  import('./pages/LoanProductsPage').then((m) => ({ default: m.LoanProductsPage }))
)
const CreateLoanProductPage = lazy(() =>
  import('./pages/CreateLoanProductPage').then((m) => ({ default: m.CreateLoanProductPage }))
)
const CashPage = lazy(() =>
  import('./pages/CashPage').then((m) => ({ default: m.CashPage }))
)
const CollectionsPage = lazy(() =>
  import('./pages/CollectionsPage').then((m) => ({ default: m.CollectionsPage }))
)
const SelectTenantPage = lazy(() =>
  import('./pages/SelectTenantPage').then((m) => ({ default: m.SelectTenantPage }))
)
const NotFoundPage = lazy(() =>
  import('./pages/NotFoundPage').then((m) => ({ default: m.NotFoundPage }))
)

function RouteFallback() {
  return (
    <div className="flex min-h-[40vh] items-center justify-center">
      <div className="text-sm text-slate-500">Cargando...</div>
    </div>
  )
}

function ProtectedRoute({ children, allowPartialAuth = false }: { children: ReactNode; allowPartialAuth?: boolean }) {
  const { isAuthenticated, isLoadingSession } = useAuth()

  if (isLoadingSession) {
    return <RouteFallback />
  }

  // allowPartialAuth permite acceso con autenticación parcial (para select-tenant)
  if (!isAuthenticated && !allowPartialAuth) {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}

export default function App() {
  return (
    <Suspense fallback={<RouteFallback />}>
      <Routes>
        {/* Public routes */}
        <Route path="/login" element={<LoginPage />} />

        <Route
          path="/select-tenant"
          element={
            <ProtectedRoute allowPartialAuth>
              <SelectTenantPage />
            </ProtectedRoute>
          }
        />

        {/* Super Admin routes */}
        <Route
          path="/admin"
          element={
            <ProtectedRoute>
              <RequirePlatformAdmin>
                <AppLayout />
              </RequirePlatformAdmin>
            </ProtectedRoute>
          }
        >
          <Route index element={<SuperAdminDashboardPage />} />
          <Route path="organizations" element={<OrganizationsPage />} />
          <Route path="organizations/:id" element={<OrganizationDetailPage />} />
          <Route path="settings" element={<SettingsPage />} />
        </Route>

        {/* Protected routes */}
        <Route
          element={
            <ProtectedRoute>
              <AppLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<DashboardHomePage />} />

          {/* Customers */}
          <Route
            path="customers"
            element={
              <RequirePermission permission="customers.view">
                <CustomersPage />
              </RequirePermission>
            }
          />
          <Route
            path="customers/new"
            element={
              <RequirePermission permission="customers.manage">
                <CreateCustomerPage />
              </RequirePermission>
            }
          />
          <Route
            path="customers/:id"
            element={
              <RequirePermission permission="customers.view">
                <CustomerDetailPage />
              </RequirePermission>
            }
          />

          {/* Loans */}
          <Route
            path="loans"
            element={
              <RequirePermission permission="loans.view">
                <LoansPage />
              </RequirePermission>
            }
          />
          <Route
            path="loans/new"
            element={
              <RequirePermission permission="loans.manage">
                <CreateLoanPage />
              </RequirePermission>
            }
          />
          <Route
            path="loans/:id"
            element={
              <RequirePermission permission="loans.view">
                <LoanDetailPage />
              </RequirePermission>
            }
          />

          {/* Loan Products */}
          <Route
            path="loan-products"
            element={
              <RequirePermission permission="loans.view">
                <LoanProductsPage />
              </RequirePermission>
            }
          />
          <Route
            path="loan-products/new"
            element={
              <RequirePermission permission="loans.manage">
                <CreateLoanProductPage />
              </RequirePermission>
            }
          />
          <Route
            path="loan-products/:id"
            element={
              <RequirePermission permission="loans.manage">
                <CreateLoanProductPage />
              </RequirePermission>
            }
          />

          {/* Cash */}
          <Route
            path="cash"
            element={
              <RequirePermission permission="cash.manage">
                <CashPage />
              </RequirePermission>
            }
          />

          {/* Collections */}
          <Route
            path="collections"
            element={
              <RequirePermission permission="collections.manage">
                <CollectionsPage />
              </RequirePermission>
            }
          />

          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </Suspense>
  )
}
