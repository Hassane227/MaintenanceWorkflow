export type RuntimeRole = 'Gestionnaire' | 'GestionnairePrincipal' | 'Intervenant'

export interface Workflow {
  id: number
  name: string
  isActive: boolean
}

export interface Company {
  id: number
  name: string
  curativeWorkflowId: number | null
  curativeWorkflowName: string | null
}

export interface WorkflowStatus {
  id: number
  workflowId: number
  name: string
  order: number
  isInitial: boolean
  isFinal: boolean
  isActive: boolean
}

export interface WorkflowTransition {
  id: number
  workflowId: number
  fromStatusId: number
  fromStatusName: string
  toStatusId: number
  toStatusName: string
  actionName: string
  roleAllowed: RuntimeRole
  isActive: boolean
}

export interface NonConformityListItem {
  id: number
  title: string
  companyName: string
  workflowName: string
  currentStatusName: string
  createdAtUtc: string
}

export interface StatusHistory {
  id: number
  fromStatusId: number
  fromStatusName: string
  toStatusId: number
  toStatusName: string
  actionName: string
  roleUsed: RuntimeRole
  performedBy: string
  dateUtc: string
}

export interface NonConformityDetails {
  id: number
  title: string
  companyId: number
  companyName: string
  workflowId: number
  workflowName: string
  currentStatusId: number
  currentStatusName: string
  createdAtUtc: string
  history: StatusHistory[]
}

export interface AvailableAction {
  transitionId: number
  actionName: string
  toStatusId: number
  toStatusName: string
}
