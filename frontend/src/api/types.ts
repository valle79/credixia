// ============================================
// Type Definitions from Backend API
// ============================================

// Auth & Session
export interface LoginRequest {
  email: string
  password: string
  tenantSlug?: string
}

export interface LoginResponse {
  requiresTenantSelection: boolean
  user?: SessionUser
  tenants?: TenantOption[]
  session?: SessionResponse
}

export interface SessionResponse {
  accessToken: string
  expiresAt: string
  user: SessionUser
  tenant?: TenantSummary
  permissions: string[]
}

export interface SessionUser {
  id: string
  email: string
  fullName: string
  isPlatformAdmin: boolean
}

export interface TenantOption {
  id: string
  slug: string
  name: string
}

export interface TenantSummary {
  id: string
  slug: string
  name: string
}

export interface MeResponse {
  user: SessionUser
  tenant?: TenantSummary
  permissions: string[]
}

// Customers
export interface CustomerSummary {
  id: string
  type: string
  documentType: string
  documentNumber: string
  fullName: string
  phone?: string
  status: string
  createdAt: string
}

export interface CustomerDetails extends CustomerSummary {
  birthDate?: string
  email?: string
  address?: string
  district?: string
  province?: string
  department?: string
  occupation?: string
  employer?: string
  monthlyIncome?: number
  notes?: string
  references: CustomerReference[]
  updatedAt: string
}

export interface CustomerReference {
  id: string
  fullName: string
  relationship?: string | undefined
  phone: string
  address?: string | undefined
}

export type CustomerReferenceDto = CustomerReference

export interface CreateCustomerRequest {
  type: 'Natural' | 'Juridica'
  documentType: string
  documentNumber: string
  fullName: string
  birthDate?: string | undefined
  phone?: string | undefined
  email?: string | undefined
  address?: string | undefined
  district?: string | undefined
  province?: string | undefined
  department?: string | undefined
  occupation?: string | undefined
  employer?: string | undefined
  monthlyIncome?: number | undefined
}

// Loan Products
export interface LoanProductSummary {
  id: string
  code: string
  name: string
  frequency: string
  interestRate: number
  interestType: string
  minimumAmount: number
  maximumAmount: number
  minimumTermDays: number
  maximumTermDays: number
  currency: string
  description?: string
  isActive: boolean
}

export interface LoanProductDetails extends LoanProductSummary {
  commissionPercentage?: number
  fixedCommission?: number
  requiresGuarantor: boolean
  createdAt: string
}

export interface CreateLoanProductRequest {
  code: string
  name: string
  frequency: 'Daily' | 'Weekly' | 'Biweekly' | 'Monthly' | 'Quarterly'
  interestRate: number
  interestType: 'Simple' | 'Compound'
  minimumAmount: number
  maximumAmount: number
  minimumTermDays: number
  maximumTermDays: number
  currency: 'PEN' | 'USD'
  commissionPercentage?: number | undefined
  fixedCommission?: number | undefined
  requiresGuarantor?: boolean | undefined
  description?: string | undefined
}

// Loans
export interface LoanSummary {
  id: string
  loanNumber: string
  customerId: string
  customerName: string
  principalAmount: number
  totalAmount: number
  balanceAmount: number
  currency: string
  interestRate: number
  status: string
  disbursementDate: string
  createdAt: string
}

export interface LoanSummaryDto extends LoanSummary {
  productName?: string
  customerPhone?: string
  nextPaymentDueDate?: string
  totalDue?: number
  lastCollectionDate?: string
}

export interface LoanDetails {
  id: string
  loanNumber: string
  customerId: string
  customerName: string
  customerPhone?: string
  loanProductId: string
  productName: string
  principalAmount: number
  interestRate: number
  interestType: string
  frequency: string
  termDays: number
  numberOfPayments: number
  disbursementDate: string
  firstPaymentDate: string
  commission?: number
  totalInterest: number
  totalAmount: number
  paidAmount: number
  balanceAmount: number
  currency: string
  status: string
  approvedDate?: string
  disbursedDate?: string
  closedDate?: string
  notes?: string
  createdAt: string
  updatedAt: string
}

export interface CreateLoanRequest {
  customerId: string
  loanProductId: string
  principalAmount: number
  termDays: number
  numberOfPayments: number
  disbursementDate: string
  firstPaymentDate: string
  commissionAmount?: number
  customInterestRate?: number
}

export interface RegisterPaymentRequest {
  receiptNumber: string
  amount: number
  paymentDate: string
}

export interface PaymentScheduleItem {
  id: string
  paymentNumber: number
  dueDate: string
  dueAmount: number
  paidAmount: number
  lateFee: number
  paidDate?: string
  isPaid: boolean
  isOverdue: boolean
  principalAmount?: number
  interestAmount?: number
  remainingBalance?: number
}

// Memberships
export interface MembershipSummary {
  id: string
  userId: string
  email: string
  fullName: string
  status: string
  roles: RoleSummary[]
  createdAt: string
}

export interface RoleSummary {
  id: string
  code: string
  name: string
}

export interface InvitationSummary {
  id: string
  email: string
  roleCodes: string[]
  createdAt: string
  expiresAt: string
}

// Tenants
export interface CurrentTenantDetails {
  tenant: {
    id: string
    slug: string
    name: string
    status: string
  }
  settings?: {
    timezone: string
    currency: string
    locale: string
    businessDayRule: string
  }
}

export interface PlatformTenantSummary {
  id: string
  slug: string
  name: string
  status: string
  activeMembers: number
  createdAt: string
}

export interface CreateTenantResponse {
  id: string
  slug: string
  name: string
  status: string
  activeMembers: number
  createdAt: string
  ownerEmail: string
  generatedPassword: string
  ownerPhone: string
}

export interface TenantDetails {
  id: string
  slug: string
  name: string
  status: string
  memberCount: number
  createdAt: string
  ownerEmail: string
  ownerFullName: string
  ownerPhone: string
}

export interface UpdateTenantRequest {
  name: string
  ownerEmail?: string
  ownerFullName?: string
  ownerPhone?: string
}

export interface ResetPasswordResponse {
  email: string
  newPassword: string
}

export interface PlatformDashboard {
  totalTenants: number
  activeTenants: number
  suspendedTenants: number
  totalUsers: number
  activeUsers: number
  totalMembers: number
  recentTenants: RecentTenant[]
}

export interface RecentTenant {
  id: string
  slug: string
  name: string
  status: string
  createdAt: string
}

export interface WhatsAppCredentialsResponse {
  whatsAppUrl: string
  email: string
  phoneNumber: string
  tenantName: string
}

// Pagination
export interface PaginatedResponseDto<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage?: boolean
  hasNextPage?: boolean
}

export type PaginatedResponse<T> = PaginatedResponseDto<T>

// Dashboard
export interface PendingLoanSummary {
  id: string
  loanNumber: string
  customerId: string
  customerName: string
  principalAmount: number
  currency: string
  createdAt: string
}

export interface DashboardSummary {
  activePortfolio: number
  activeLoanCount: number
  activePrincipal: number
  activeInterest: number
  disbursedThisMonth: number
  activeCustomerCount: number
  pendingLoanCount: number
  overdueBalance: number
  overdueLoanCount: number
  pendingLoans: PendingLoanSummary[]
}

// API Error
export interface ApiError {
  type: string
  title: string
  status: number
  detail?: string
  errors?: Record<string, string[]>
}

// Collections
export interface OverdueLoansResponse {
  items: LoanSummaryDto[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalOverdueAmount: number
  totalPages: number
  hasPreviousPage?: boolean
  hasNextPage?: boolean
}

export interface CollectionActionDto {
  id: string
  loanId: string
  loanNumber: string
  customerName: string
  actionType: string
  actionDate: string
  notes?: string
  followUpDate?: string
  performedBy: string
  createdAt: string
}

// Cash Management
export interface CashSessionDto {
  id: string
  sessionNumber: string
  openedAt: string
  openedBy: string
  closedAt?: string
  closedBy?: string
  openingBalance: number
  closingBalance?: number
  currentBalance: number
  totalIn: number
  totalOut: number
  expectedBalance: number
  actualBalance?: number
  difference?: number
  status: string
  notes?: string
}

export interface CashTransactionDto {
  id: string
  cashSessionId: string
  transactionType: string
  direction: 'Inflow' | 'Outflow'
  amount: number
  currency: string
  reference?: string
  notes?: string
  createdAt: string
  createdBy: string
}
