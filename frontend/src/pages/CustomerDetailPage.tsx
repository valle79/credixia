import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { customersApi } from '../api/customers.api';
import { loansApi } from '../api/loans.api';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Badge } from '../components/ui/Badge';
import { Card } from '../components/ui/Card';
import { formatCurrency, formatDate } from '../lib/format';
import { useToast } from '../hooks/useToast';
import { useAuth } from '../hooks/useAuth';
import { ToastContainer } from '../components/ui/ToastContainer';
import { getErrorMessage } from '../api/client';
import type { CustomerReferenceDto } from '../api/types';

type CustomerStatus = 'Active' | 'Suspended' | 'Blocked';

export function CustomerDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const toast = useToast();
  const { tenant } = useAuth();
  const [isEditing, setIsEditing] = useState(false);
  const [showAddReference, setShowAddReference] = useState(false);

  const { data: customer, isLoading } = useQuery({
    queryKey: ['customer', tenant?.id, id],
    queryFn: () => customersApi.get(id!),
    enabled: !!tenant?.id && !!id,
  });

  const { data: customerLoans } = useQuery({
    queryKey: ['loans', tenant?.id, { customerId: id }],
    queryFn: () => loansApi.list({ customerId: id, pageSize: 100 }),
    enabled: !!tenant?.id && !!id,
  });

  const updateMutation = useMutation({
    mutationFn: (data: Partial<import('../api/types').CreateCustomerRequest>) =>
      customersApi.update(id!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customer', tenant?.id, id] });
      setIsEditing(false);
      toast.success('Cliente actualizado', 'Los datos del cliente han sido actualizados exitosamente');
    },
    onError: (error) => {
      toast.error('Error al actualizar', getErrorMessage(error));
    },
  });

  const updateStatusMutation = useMutation({
    mutationFn: (status: CustomerStatus) => customersApi.updateStatus(id!, status),
    onSuccess: (_, status) => {
      queryClient.invalidateQueries({ queryKey: ['customer', tenant?.id, id] });
      const statusMessages = {
        Active: 'Cliente reactivado exitosamente',
        Suspended: 'Cliente suspendido exitosamente',
        Blocked: 'Cliente bloqueado exitosamente',
      };
      toast.success('Estado actualizado', statusMessages[status]);
    },
    onError: (error) => {
      toast.error('Error al cambiar estado', getErrorMessage(error));
    },
  });

  const addReferenceMutation = useMutation({
    mutationFn: (data: Omit<CustomerReferenceDto, 'id'>) =>
      customersApi.addReference(id!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customer', tenant?.id, id] });
      setShowAddReference(false);
      toast.success('Referencia agregada', 'La referencia personal ha sido registrada');
    },
    onError: (error) => {
      toast.error('Error al agregar referencia', getErrorMessage(error));
    },
  });

  const removeReferenceMutation = useMutation({
    mutationFn: (referenceId: string) => customersApi.removeReference(id!, referenceId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['customer', tenant?.id, id] });
      toast.success('Referencia eliminada', 'La referencia personal ha sido eliminada');
    },
    onError: (error) => {
      toast.error('Error al eliminar referencia', getErrorMessage(error));
    },
  });

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const value = (name: string) => {
      const entry = formData.get(name);
      return entry ? String(entry) : undefined;
    };

    updateMutation.mutate({
      fullName: value('fullName') ?? '',
      birthDate: value('birthDate') || undefined,
      email: value('email') || undefined,
      phone: value('phone') || undefined,
      address: value('address') || undefined,
      district: value('district') || undefined,
      province: value('province') || undefined,
      department: value('department') || undefined,
      occupation: value('occupation') || undefined,
      employer: value('employer') || undefined,
      monthlyIncome: value('monthlyIncome') ? Number(value('monthlyIncome')) : undefined,
    });
  };

  const handleAddReference = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    addReferenceMutation.mutate({
      fullName: formData.get('fullName') as string,
      relationship: (formData.get('relationship') as string) || undefined,
      phone: formData.get('phone') as string,
    });
  };

  if (isLoading) {
    return <div className="p-6">Cargando...</div>;
  }

  if (!customer) {
    return <div className="p-6">Cliente no encontrado</div>;
  }

  const getStatusBadge = (status: string) => {
    const variants: Record<string, 'success' | 'warning' | 'danger' | 'default'> = {
      Active: 'success',
      Suspended: 'warning',
      Blocked: 'danger',
    };
    return <Badge variant={variants[status] || 'default'}>{status}</Badge>;
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <Button variant="ghost" onClick={() => navigate('/customers')}>
            ← Volver
          </Button>
          <h1 className="text-2xl font-semibold mt-2">{customer.fullName}</h1>
          <p className="text-gray-600">
            {customer.documentType} {customer.documentNumber}
          </p>
        </div>
        <div className="flex items-center gap-3">
          {getStatusBadge(customer.status)}
          {!isEditing && (
            <Button onClick={() => setIsEditing(true)}>Editar</Button>
          )}
        </div>
      </div>

      {/* Main Info */}
      <Card>
        <h2 className="text-lg font-semibold mb-4">Información Personal</h2>
        {isEditing ? (
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Nombre Completo"
                name="fullName"
                defaultValue={customer.fullName}
                required
              />
              <Input
                label="Fecha de Nacimiento"
                name="birthDate"
                type="date"
                defaultValue={customer.birthDate || ''}
              />
              <Input
                label="Email"
                name="email"
                type="email"
                defaultValue={customer.email || ''}
              />
              <Input
                label="Teléfono"
                name="phone"
                defaultValue={customer.phone || ''}
              />
              <Input
                label="Distrito"
                name="district"
                defaultValue={customer.district || ''}
              />
              <Input
                label="Provincia"
                name="province"
                defaultValue={customer.province || ''}
              />
              <Input
                label="Departamento"
                name="department"
                defaultValue={customer.department || ''}
              />
              <Input
                label="Ocupación"
                name="occupation"
                defaultValue={customer.occupation || ''}
              />
            </div>
            <Input
              label="Dirección"
              name="address"
              defaultValue={customer.address || ''}
            />
            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Empleador"
                name="employer"
                defaultValue={customer.employer || ''}
              />
              <Input
                label="Ingreso Mensual"
                name="monthlyIncome"
                type="number"
                step="0.01"
                defaultValue={customer.monthlyIncome ?? ''}
              />
            </div>
            <div className="flex gap-3">
              <Button type="submit" disabled={updateMutation.isPending}>
                {updateMutation.isPending ? 'Guardando...' : 'Guardar'}
              </Button>
              <Button
                type="button"
                variant="secondary"
                onClick={() => setIsEditing(false)}
              >
                Cancelar
              </Button>
            </div>
          </form>
        ) : (
          <div className="grid grid-cols-2 gap-4">
            <div>
              <p className="text-sm text-gray-600">Email</p>
              <p className="font-medium">{customer.email || '-'}</p>
            </div>
            <div>
              <p className="text-sm text-gray-600">Teléfono</p>
              <p className="font-medium">{customer.phone || '-'}</p>
            </div>
            <div>
              <p className="text-sm text-gray-600">Dirección</p>
              <p className="font-medium">{customer.address || '-'}</p>
            </div>
            <div>
              <p className="text-sm text-gray-600">Distrito / Provincia / Departamento</p>
              <p className="font-medium">
                {[customer.district, customer.province, customer.department]
                  .filter(Boolean)
                  .join(' / ') || '-'}
              </p>
            </div>
            <div>
              <p className="text-sm text-gray-600">Ocupación</p>
              <p className="font-medium">{customer.occupation || '-'}</p>
            </div>
            <div>
              <p className="text-sm text-gray-600">Empleador</p>
              <p className="font-medium">{customer.employer || '-'}</p>
            </div>
            <div>
              <p className="text-sm text-gray-600">Ingreso Mensual</p>
              <p className="font-medium">
                {customer.monthlyIncome ? formatCurrency(customer.monthlyIncome) : '-'}
              </p>
            </div>
            <div>
              <p className="text-sm text-gray-600">Fecha de Registro</p>
              <p className="font-medium">{formatDate(customer.createdAt)}</p>
            </div>
          </div>
        )}
      </Card>

      {/* Status Actions */}
      {customer.status !== 'Blocked' && (
        <Card>
          <h2 className="text-lg font-semibold mb-4">Acciones</h2>
          <div className="flex gap-3">
            {customer.status === 'Active' && (
              <Button
                variant="secondary"
                onClick={() => updateStatusMutation.mutate('Suspended')}
                disabled={updateStatusMutation.isPending}
              >
                Suspender Cliente
              </Button>
            )}
            {customer.status === 'Suspended' && (
              <Button
                onClick={() => updateStatusMutation.mutate('Active')}
                disabled={updateStatusMutation.isPending}
              >
                Reactivar Cliente
              </Button>
            )}
            <Button
              variant="danger"
              onClick={() => {
                if (confirm('¿Está seguro de bloquear este cliente? Esta acción es permanente.')) {
                  updateStatusMutation.mutate('Blocked');
                }
              }}
              disabled={updateStatusMutation.isPending}
            >
              Bloquear Cliente
            </Button>
          </div>
        </Card>
      )}

      {/* References */}
      <Card>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold">Referencias Personales</h2>
          {!showAddReference && (
            <Button onClick={() => setShowAddReference(true)}>
              + Agregar Referencia
            </Button>
          )}
        </div>

        {showAddReference && (
          <form onSubmit={handleAddReference} className="mb-6 p-4 bg-gray-50 rounded-lg space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Input label="Nombre Completo" name="fullName" required />
              <Input label="Parentesco" name="relationship" />
              <Input label="Teléfono" name="phone" required />
            </div>
            <div className="flex gap-3">
              <Button type="submit" disabled={addReferenceMutation.isPending}>
                {addReferenceMutation.isPending ? 'Guardando...' : 'Guardar'}
              </Button>
              <Button
                type="button"
                variant="secondary"
                onClick={() => setShowAddReference(false)}
              >
                Cancelar
              </Button>
            </div>
          </form>
        )}

        {customer.references && customer.references.length > 0 ? (
          <div className="space-y-3">
            {customer.references.map((ref) => (
              <div key={ref.id} className="p-4 border rounded-lg flex justify-between items-start">
                <div>
                  <p className="font-medium">{ref.fullName}</p>
                  {ref.relationship && (
                    <p className="text-sm text-gray-600">{ref.relationship}</p>
                  )}
                  <p className="text-sm text-gray-600">{ref.phone}</p>
                </div>
                <Button
                  variant="danger"
                  size="sm"
                  onClick={() => {
                    if (confirm('¿Eliminar esta referencia?')) {
                      removeReferenceMutation.mutate(ref.id);
                    }
                  }}
                  disabled={removeReferenceMutation.isPending}
                >
                  Eliminar
                </Button>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-gray-600">No hay referencias registradas</p>
        )}
      </Card>

      {/* Loans */}
      <Card>
        <h2 className="text-lg font-semibold mb-4">Préstamos</h2>
        {customerLoans && customerLoans.items.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-2 text-left text-sm font-medium">Número</th>
                  <th className="px-4 py-2 text-left text-sm font-medium">Monto</th>
                  <th className="px-4 py-2 text-left text-sm font-medium">Saldo</th>
                  <th className="px-4 py-2 text-left text-sm font-medium">Estado</th>
                  <th className="px-4 py-2 text-left text-sm font-medium">Fecha</th>
                  <th className="px-4 py-2"></th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {customerLoans.items.map((loan) => (
                  <tr key={loan.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 text-sm font-mono">{loan.loanNumber}</td>
                    <td className="px-4 py-3 text-sm font-medium">
                      {formatCurrency(loan.principalAmount, loan.currency)}
                    </td>
                    <td className="px-4 py-3 text-sm">
                      {formatCurrency(loan.balanceAmount, loan.currency)}
                    </td>
                    <td className="px-4 py-3 text-sm">
                      <Badge variant={loan.status === 'Active' ? 'success' : 'default'}>
                        {loan.status}
                      </Badge>
                    </td>
                    <td className="px-4 py-3 text-sm">{formatDate(loan.createdAt)}</td>
                    <td className="px-4 py-3 text-sm">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => navigate(`/loans/${loan.id}`)}
                      >
                        Ver
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="text-center py-8">
            <p className="text-gray-600 mb-4">No hay préstamos registrados</p>
            <Button onClick={() => navigate(`/loans/new?customerId=${id}`)}>
              Crear Préstamo
            </Button>
          </div>
        )}
      </Card>
      <ToastContainer
  toasts={toast.toasts}
  onClose={toast.close}
/>
    </div>
  );
}

