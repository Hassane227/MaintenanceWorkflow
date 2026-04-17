import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'
import { apiRequest, ApiError } from './api'
import type {
  AvailableAction,
  Company,
  NonConformityDetails,
  NonConformityListItem,
  RuntimeRole,
  Workflow,
  WorkflowStatus,
  WorkflowTransition,
} from './types'

const ROLES: RuntimeRole[] = ['Gestionnaire', 'GestionnairePrincipal', 'Intervenant']
const ROLE_STORAGE_KEY = 'maintenanceworkflow:selected-role'

type Route =
  | { page: 'companies' }
  | { page: 'workflows' }
  | { page: 'statuses'; workflowId: number }
  | { page: 'transitions'; workflowId: number }
  | { page: 'ncs' }
  | { page: 'nc-details'; ncId: number }

function parseRoute(pathname: string): Route {
  if (pathname === '/' || pathname === '/companies') {
    return { page: 'companies' }
  }

  if (pathname === '/workflows') {
    return { page: 'workflows' }
  }

  const statusesMatch = pathname.match(/^\/workflows\/(\d+)\/statuses$/)
  if (statusesMatch) {
    return { page: 'statuses', workflowId: Number(statusesMatch[1]) }
  }

  const transitionsMatch = pathname.match(/^\/workflows\/(\d+)\/transitions$/)
  if (transitionsMatch) {
    return { page: 'transitions', workflowId: Number(transitionsMatch[1]) }
  }

  if (pathname === '/ncs') {
    return { page: 'ncs' }
  }

  const ncDetailsMatch = pathname.match(/^\/ncs\/(\d+)$/)
  if (ncDetailsMatch) {
    return { page: 'nc-details', ncId: Number(ncDetailsMatch[1]) }
  }

  return { page: 'companies' }
}

function formatDate(value: string): string {
  return new Date(value).toLocaleString()
}

function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'Unexpected error.'
}

function App() {
  const [pathname, setPathname] = useState(window.location.pathname)
  const [selectedRole, setSelectedRole] = useState<RuntimeRole>(() => {
    const stored = localStorage.getItem(ROLE_STORAGE_KEY)
    return ROLES.includes(stored as RuntimeRole) ? (stored as RuntimeRole) : 'Gestionnaire'
  })

  useEffect(() => {
    const handler = () => setPathname(window.location.pathname)
    window.addEventListener('popstate', handler)
    return () => window.removeEventListener('popstate', handler)
  }, [])

  useEffect(() => {
    localStorage.setItem(ROLE_STORAGE_KEY, selectedRole)
  }, [selectedRole])

  const route = useMemo(() => parseRoute(pathname), [pathname])

  const navigate = (path: string) => {
    if (path !== window.location.pathname) {
      window.history.pushState({}, '', path)
      setPathname(path)
    }
  }

  return (
    <div className="app">
      <header>
        <h1>MaintenanceWorkflow MVP</h1>
        <nav>
          <button onClick={() => navigate('/companies')}>Companies</button>
          <button onClick={() => navigate('/workflows')}>Workflows</button>
          <button onClick={() => navigate('/ncs')}>NCs</button>
        </nav>
      </header>

      {route.page === 'companies' && <CompaniesPage />}
      {route.page === 'workflows' && <WorkflowsPage onNavigate={navigate} />}
      {route.page === 'statuses' && <StatusesPage workflowId={route.workflowId} />}
      {route.page === 'transitions' && <TransitionsPage workflowId={route.workflowId} />}
      {route.page === 'ncs' && <NcListPage onNavigate={navigate} />}
      {route.page === 'nc-details' && (
        <NcDetailsPage ncId={route.ncId} role={selectedRole} onRoleChange={setSelectedRole} />
      )}
    </div>
  )
}

function CompaniesPage() {
  const [companies, setCompanies] = useState<Company[]>([])
  const [workflows, setWorkflows] = useState<Workflow[]>([])
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)

  const load = async () => {
    const [companiesData, workflowsData] = await Promise.all([
      apiRequest<Company[]>('/api/admin/companies'),
      apiRequest<Workflow[]>('/api/admin/workflows'),
    ])
    setCompanies(companiesData)
    setWorkflows(workflowsData)
  }

  useEffect(() => {
    load().catch((err: unknown) => setError(errorMessage(err)))
  }, [])

  const onCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)

    try {
      await apiRequest<Company>('/api/admin/companies', {
        method: 'POST',
        body: JSON.stringify({ name }),
      })
      setName('')
      await load()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const onAssign = async (companyId: number, workflowId: string) => {
    setError(null)

    try {
      await apiRequest<Company>(`/api/admin/companies/${companyId}/curative-workflow`, {
        method: 'PUT',
        body: JSON.stringify({ workflowId: Number(workflowId) }),
      })
      await load()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <section>
      <h2>Companies</h2>
      <form onSubmit={onCreate} className="inline-form">
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="New company name" required />
        <button type="submit">Create</button>
      </form>
      {error && <p className="error">{error}</p>}

      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Curative Workflow</th>
            <th>Assign</th>
          </tr>
        </thead>
        <tbody>
          {companies.map((company) => (
            <tr key={company.id}>
              <td>{company.name}</td>
              <td>{company.curativeWorkflowName ?? '-'}</td>
              <td>
                <select
                  value={company.curativeWorkflowId ?? ''}
                  onChange={(e) => {
                    if (e.target.value) {
                      onAssign(company.id, e.target.value).catch((err: unknown) => setError(errorMessage(err)))
                    }
                  }}
                >
                  <option value="">Select workflow</option>
                  {workflows.map((workflow) => (
                    <option key={workflow.id} value={workflow.id}>
                      {workflow.name}
                    </option>
                  ))}
                </select>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}

function WorkflowsPage({ onNavigate }: { onNavigate: (path: string) => void }) {
  const [workflows, setWorkflows] = useState<Workflow[]>([])
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)

  const load = async () => {
    setWorkflows(await apiRequest<Workflow[]>('/api/admin/workflows'))
  }

  useEffect(() => {
    load().catch((err: unknown) => setError(errorMessage(err)))
  }, [])

  const onCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)

    try {
      await apiRequest<Workflow>('/api/admin/workflows', {
        method: 'POST',
        body: JSON.stringify({ name }),
      })
      setName('')
      await load()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <section>
      <h2>Workflows</h2>
      <form onSubmit={onCreate} className="inline-form">
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="New workflow name" required />
        <button type="submit">Create</button>
      </form>
      {error && <p className="error">{error}</p>}

      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Statuses</th>
            <th>Transitions</th>
          </tr>
        </thead>
        <tbody>
          {workflows.map((workflow) => (
            <tr key={workflow.id}>
              <td>{workflow.name}</td>
              <td>
                <button onClick={() => onNavigate(`/workflows/${workflow.id}/statuses`)}>Manage statuses</button>
              </td>
              <td>
                <button onClick={() => onNavigate(`/workflows/${workflow.id}/transitions`)}>Manage transitions</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}

function StatusesPage({ workflowId }: { workflowId: number }) {
  const [statuses, setStatuses] = useState<WorkflowStatus[]>([])
  const [form, setForm] = useState({ name: '', order: 1, isInitial: false, isFinal: false, isActive: true })
  const [editingId, setEditingId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  const load = async () => {
    setStatuses(await apiRequest<WorkflowStatus[]>(`/api/admin/workflows/${workflowId}/statuses`))
  }

  useEffect(() => {
    load().catch((err: unknown) => setError(errorMessage(err)))
  }, [workflowId])

  const reset = () => {
    setForm({ name: '', order: 1, isInitial: false, isFinal: false, isActive: true })
    setEditingId(null)
  }

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)

    try {
      if (editingId) {
        await apiRequest<WorkflowStatus>(`/api/admin/statuses/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify(form),
        })
      } else {
        await apiRequest<WorkflowStatus>(`/api/admin/workflows/${workflowId}/statuses`, {
          method: 'POST',
          body: JSON.stringify(form),
        })
      }

      reset()
      await load()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const onDelete = async (statusId: number) => {
    setError(null)
    try {
      await apiRequest<void>(`/api/admin/statuses/${statusId}`, { method: 'DELETE' })
      await load()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <section>
      <h2>Workflow #{workflowId} - Statuses</h2>
      <form onSubmit={onSubmit} className="grid-form">
        <input
          value={form.name}
          onChange={(e) => setForm((prev) => ({ ...prev, name: e.target.value }))}
          placeholder="Status name"
          required
        />
        <input
          type="number"
          value={form.order}
          onChange={(e) => setForm((prev) => ({ ...prev, order: Number(e.target.value) }))}
          min={0}
          required
        />
        <label><input type="checkbox" checked={form.isInitial} onChange={(e) => setForm((prev) => ({ ...prev, isInitial: e.target.checked }))} />Initial</label>
        <label><input type="checkbox" checked={form.isFinal} onChange={(e) => setForm((prev) => ({ ...prev, isFinal: e.target.checked }))} />Final</label>
        <label><input type="checkbox" checked={form.isActive} onChange={(e) => setForm((prev) => ({ ...prev, isActive: e.target.checked }))} />Active</label>
        <button type="submit">{editingId ? 'Update' : 'Add'}</button>
        {editingId && <button type="button" onClick={reset}>Cancel</button>}
      </form>
      {error && <p className="error">{error}</p>}

      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Order</th>
            <th>Initial</th>
            <th>Final</th>
            <th>Active</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {statuses.map((status) => (
            <tr key={status.id}>
              <td>{status.name}</td>
              <td>{status.order}</td>
              <td>{status.isInitial ? 'Yes' : 'No'}</td>
              <td>{status.isFinal ? 'Yes' : 'No'}</td>
              <td>{status.isActive ? 'Yes' : 'No'}</td>
              <td className="actions">
                <button onClick={() => { setEditingId(status.id); setForm({ name: status.name, order: status.order, isInitial: status.isInitial, isFinal: status.isFinal, isActive: status.isActive }) }}>Edit</button>
                <button onClick={() => onDelete(status.id).catch((err: unknown) => setError(errorMessage(err)))}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}

function TransitionsPage({ workflowId }: { workflowId: number }) {
  const [statuses, setStatuses] = useState<WorkflowStatus[]>([])
  const [transitions, setTransitions] = useState<WorkflowTransition[]>([])
  const [form, setForm] = useState({ fromStatusId: 0, toStatusId: 0, actionName: '', roleAllowed: 'Gestionnaire' as RuntimeRole, isActive: true })
  const [editingId, setEditingId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  const load = async () => {
    const [statusData, transitionData] = await Promise.all([
      apiRequest<WorkflowStatus[]>(`/api/admin/workflows/${workflowId}/statuses`),
      apiRequest<WorkflowTransition[]>(`/api/admin/workflows/${workflowId}/transitions`),
    ])

    setStatuses(statusData)
    setTransitions(transitionData)

    if (statusData.length > 0 && form.fromStatusId === 0) {
      setForm((prev) => ({ ...prev, fromStatusId: statusData[0].id, toStatusId: statusData[0].id }))
    }
  }

  useEffect(() => {
    load().catch((err: unknown) => setError(errorMessage(err)))
  }, [workflowId])

  const reset = () => {
    const fallback = statuses[0]?.id ?? 0
    setForm({ fromStatusId: fallback, toStatusId: fallback, actionName: '', roleAllowed: 'Gestionnaire', isActive: true })
    setEditingId(null)
  }

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)

    try {
      if (editingId) {
        await apiRequest<WorkflowTransition>(`/api/admin/transitions/${editingId}`, {
          method: 'PUT',
          body: JSON.stringify(form),
        })
      } else {
        await apiRequest<WorkflowTransition>(`/api/admin/workflows/${workflowId}/transitions`, {
          method: 'POST',
          body: JSON.stringify(form),
        })
      }

      reset()
      await load()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const onDelete = async (transitionId: number) => {
    setError(null)
    try {
      await apiRequest<void>(`/api/admin/transitions/${transitionId}`, { method: 'DELETE' })
      await load()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <section>
      <h2>Workflow #{workflowId} - Transitions</h2>
      <form onSubmit={onSubmit} className="grid-form">
        <select value={form.fromStatusId} onChange={(e) => setForm((prev) => ({ ...prev, fromStatusId: Number(e.target.value) }))}>
          {statuses.map((status) => <option key={status.id} value={status.id}>{status.name}</option>)}
        </select>
        <input value={form.actionName} onChange={(e) => setForm((prev) => ({ ...prev, actionName: e.target.value }))} placeholder="Action name" required />
        <select value={form.toStatusId} onChange={(e) => setForm((prev) => ({ ...prev, toStatusId: Number(e.target.value) }))}>
          {statuses.map((status) => <option key={status.id} value={status.id}>{status.name}</option>)}
        </select>
        <select value={form.roleAllowed} onChange={(e) => setForm((prev) => ({ ...prev, roleAllowed: e.target.value as RuntimeRole }))}>
          {ROLES.map((role) => <option key={role} value={role}>{role}</option>)}
        </select>
        <label><input type="checkbox" checked={form.isActive} onChange={(e) => setForm((prev) => ({ ...prev, isActive: e.target.checked }))} />Active</label>
        <button type="submit">{editingId ? 'Update' : 'Add'}</button>
        {editingId && <button type="button" onClick={reset}>Cancel</button>}
      </form>
      {error && <p className="error">{error}</p>}

      <table>
        <thead>
          <tr>
            <th>From</th>
            <th>Action</th>
            <th>To</th>
            <th>Role</th>
            <th>Active</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {transitions.map((transition) => (
            <tr key={transition.id}>
              <td>{transition.fromStatusName}</td>
              <td>{transition.actionName}</td>
              <td>{transition.toStatusName}</td>
              <td>{transition.roleAllowed}</td>
              <td>{transition.isActive ? 'Yes' : 'No'}</td>
              <td className="actions">
                <button
                  onClick={() => {
                    setEditingId(transition.id)
                    setForm({
                      fromStatusId: transition.fromStatusId,
                      toStatusId: transition.toStatusId,
                      actionName: transition.actionName,
                      roleAllowed: transition.roleAllowed,
                      isActive: transition.isActive,
                    })
                  }}
                >
                  Edit
                </button>
                <button onClick={() => onDelete(transition.id).catch((err: unknown) => setError(errorMessage(err)))}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}

function NcListPage({ onNavigate }: { onNavigate: (path: string) => void }) {
  const [companies, setCompanies] = useState<Company[]>([])
  const [ncs, setNcs] = useState<NonConformityListItem[]>([])
  const [companyId, setCompanyId] = useState<number>(0)
  const [title, setTitle] = useState('')
  const [error, setError] = useState<string | null>(null)

  const load = async () => {
    const [companiesData, ncsData] = await Promise.all([
      apiRequest<Company[]>('/api/admin/companies'),
      apiRequest<NonConformityListItem[]>('/api/nonconformities'),
    ])

    setCompanies(companiesData)
    setNcs(ncsData)

    const firstCompanyId = companiesData[0]?.id ?? 0
    if (companyId === 0 && firstCompanyId > 0) {
      setCompanyId(firstCompanyId)
    }
  }

  useEffect(() => {
    load().catch((err: unknown) => setError(errorMessage(err)))
  }, [])

  const onCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)

    try {
      const created = await apiRequest<NonConformityDetails>('/api/nonconformities', {
        method: 'POST',
        body: JSON.stringify({ companyId, title }),
      })

      setTitle('')
      await load()
      onNavigate(`/ncs/${created.id}`)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <section>
      <h2>Non-conformities</h2>
      <form onSubmit={onCreate} className="inline-form">
        <select value={companyId} onChange={(e) => setCompanyId(Number(e.target.value))} required>
          <option value={0}>Select company</option>
          {companies.map((company) => <option key={company.id} value={company.id}>{company.name}</option>)}
        </select>
        <input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="NC title" required />
        <button type="submit">Create NC</button>
      </form>
      {error && <p className="error">{error}</p>}

      <table>
        <thead>
          <tr>
            <th>Title</th>
            <th>Company</th>
            <th>Workflow</th>
            <th>Status</th>
            <th>Created</th>
            <th>Details</th>
          </tr>
        </thead>
        <tbody>
          {ncs.map((nc) => (
            <tr key={nc.id}>
              <td>{nc.title}</td>
              <td>{nc.companyName}</td>
              <td>{nc.workflowName}</td>
              <td>{nc.currentStatusName}</td>
              <td>{formatDate(nc.createdAtUtc)}</td>
              <td><button onClick={() => onNavigate(`/ncs/${nc.id}`)}>Open</button></td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}

function NcDetailsPage({ ncId, role, onRoleChange }: { ncId: number; role: RuntimeRole; onRoleChange: (role: RuntimeRole) => void }) {
  const [nc, setNc] = useState<NonConformityDetails | null>(null)
  const [actions, setActions] = useState<AvailableAction[]>([])
  const [error, setError] = useState<string | null>(null)

  const loadNc = async (selectedRole: RuntimeRole) => {
    const [details, allowedActions] = await Promise.all([
      apiRequest<NonConformityDetails>(`/api/nonconformities/${ncId}`),
      apiRequest<AvailableAction[]>(`/api/nonconformities/${ncId}/actions?role=${selectedRole}`),
    ])

    setNc(details)
    setActions(allowedActions)
  }

  useEffect(() => {
    loadNc(role).catch((err: unknown) => setError(errorMessage(err)))
  }, [ncId, role])

  const execute = async (transitionId: number) => {
    setError(null)
    try {
      await apiRequest<NonConformityDetails>(`/api/nonconformities/${ncId}/execute`, {
        method: 'POST',
        body: JSON.stringify({ transitionId, role }),
      })
      await loadNc(role)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  if (!nc) {
    return (
      <section>
        <h2>NC #{ncId}</h2>
        {error ? <p className="error">{error}</p> : <p>Loading...</p>}
      </section>
    )
  }

  return (
    <section>
      <h2>NC #{nc.id}</h2>
      <p><strong>Title:</strong> {nc.title}</p>
      <p><strong>Company:</strong> {nc.companyName}</p>
      <p><strong>Workflow:</strong> {nc.workflowName}</p>
      <p><strong>Current status:</strong> {nc.currentStatusName}</p>

      <div className="inline-form">
        <label>
          Role:
          <select value={role} onChange={(e) => onRoleChange(e.target.value as RuntimeRole)}>
            {ROLES.map((allowedRole) => <option key={allowedRole} value={allowedRole}>{allowedRole}</option>)}
          </select>
        </label>
      </div>

      <h3>Available actions</h3>
      {actions.length === 0 && <p>No action available for this role.</p>}
      <div className="actions-list">
        {actions.map((action) => (
          <button key={action.transitionId} onClick={() => execute(action.transitionId).catch((err: unknown) => setError(errorMessage(err)))}>
            {action.actionName} → {action.toStatusName}
          </button>
        ))}
      </div>

      <h3>History</h3>
      <table>
        <thead>
          <tr>
            <th>Date</th>
            <th>From</th>
            <th>To</th>
            <th>Action</th>
            <th>Role</th>
            <th>PerformedBy</th>
          </tr>
        </thead>
        <tbody>
          {nc.history.map((item) => (
            <tr key={item.id}>
              <td>{formatDate(item.dateUtc)}</td>
              <td>{item.fromStatusName}</td>
              <td>{item.toStatusName}</td>
              <td>{item.actionName}</td>
              <td>{item.roleUsed}</td>
              <td>{item.performedBy}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {error && <p className="error">{error}</p>}
    </section>
  )
}

export default App
