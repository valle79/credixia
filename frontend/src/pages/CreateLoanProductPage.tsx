import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { loanProductsApi } from '../api/loanProducts.api';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Select } from '../components/ui/Select';
import { Card } from '../components/ui/Card';
import { useToast } from '../hooks/useToast';
import { useAuth } from '../hooks/useAuth';
import { ToastContainer } from '../components/ui/ToastContainer';
import { getErrorMessage } from '../api/client';
import type { CreateLoanProductRequest } from '../api/types';

export function CreateLoanProductPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const toast = useToast();
  const { tenant } = useAuth();
  const isEditing = !!id;

  const { data: product, isLoading } = useQuery({
    queryKey: ['loanProduct', tenant?.id, id],
    queryFn: () => loanProductsApi.get(id!),
    enabled: isEditing && !!tenant?.id,
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateLoanProductRequest) => loanProductsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['loanProducts'] });
      toast.success('Producto creado', 'El producto de préstamo ha sido creado exitosamente');
      navigate('/loan-products');
    },
    onError: (error) => {
      toast.error('Error al crear producto', getErrorMessage(error));
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: Partial<CreateLoanProductRequest>) => loanProductsApi.update(id!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['loanProducts'] });
      queryClient.invalidateQueries({ queryKey: ['loanProduct', tenant?.id, id] });
      toast.success('Producto actualizado', 'El producto de préstamo ha sido actualizado exitosamente');
      navigate('/loan-products');
    },
    onError: (error) => {
      toast.error('Error al actualizar producto', getErrorMessage(error));
    },
  });

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);

    const description = formData.get('description') as string;
    const commission = Number(formData.get('commissionPercentage'));
    const data: CreateLoanProductRequest = {
      code: formData.get('code') as string,
      name: formData.get('name') as string,
      description: description || undefined,
      frequency: formData.get('frequency') as CreateLoanProductRequest['frequency'],
      interestRate: Number(formData.get('interestRate')),
      interestType: formData.get('interestType') as CreateLoanProductRequest['interestType'],
      minimumAmount: Number(formData.get('minimumAmount')),
      maximumAmount: Number(formData.get('maximumAmount')),
      minimumTermDays: Number(formData.get('minimumTermDays')),
      maximumTermDays: Number(formData.get('maximumTermDays')),
      currency: formData.get('currency') as CreateLoanProductRequest['currency'],
      commissionPercentage: commission > 0 ? commission : undefined,
      requiresGuarantor: formData.get('requiresGuarantor') === 'on',
    };

    if (isEditing) {
      updateMutation.mutate(data);
    } else {
      createMutation.mutate(data);
    }
  };

  if (isEditing && isLoading) {
    return <div className="p-6">Cargando...</div>;
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div>
        <Button variant="ghost" onClick={() => navigate('/loan-products')}>
          ← Volver
        </Button>
        <h1 className="text-2xl font-semibold mt-2">
          {isEditing ? 'Editar Producto' : 'Nuevo Producto de Préstamo'}
        </h1>
        <p className="text-gray-600">
          {isEditing ? 'Modifica la configuración del producto' : 'Configura un nuevo producto de préstamo'}
        </p>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Basic Info */}
        <Card>
          <h2 className="text-lg font-semibold mb-4">Información Básica</h2>
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Código"
                name="code"
                placeholder="PROD001"
                defaultValue={product?.code}
                disabled={isEditing}
                required
              />
              <Input
                label="Nombre"
                name="name"
                placeholder="Préstamo Diario"
                defaultValue={product?.name}
                required
              />
            </div>
            <Input
              label="Descripción"
              name="description"
              placeholder="Descripción del producto..."
              defaultValue={product?.description}
            />
          </div>
        </Card>

        {/* Payment Configuration */}
        <Card>
          <h2 className="text-lg font-semibold mb-4">Configuración de Pagos</h2>
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Select
                label="Frecuencia de Pago"
                name="frequency"
                defaultValue={product?.frequency || 'Daily'}
                disabled={isEditing}
                required
              >
                <option value="Daily">Diario</option>
                <option value="Weekly">Semanal</option>
                <option value="Biweekly">Quincenal</option>
                <option value="Monthly">Mensual</option>
                <option value="Quarterly">Trimestral</option>
              </Select>

              <Select
                label="Tipo de Interés"
                name="interestType"
                defaultValue={product?.interestType || 'Simple'}
                disabled={isEditing}
                required
              >
                <option value="Simple">Simple</option>
                <option value="Compound">Compuesto</option>
              </Select>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Tasa de Interés (%)"
                name="interestRate"
                type="number"
                step="0.01"
                min="0"
                max="100"
                placeholder="5.00"
                defaultValue={product?.interestRate}
                required
              />
              <Select
                label="Moneda"
                name="currency"
                defaultValue={product?.currency || 'PEN'}
                required
              >
                <option value="PEN">Soles (PEN)</option>
                <option value="USD">Dólares (USD)</option>
              </Select>
            </div>
          </div>
        </Card>

        {/* Limits */}
        <Card>
          <h2 className="text-lg font-semibold mb-4">Límites</h2>
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Monto Mínimo"
                name="minimumAmount"
                type="number"
                step="0.01"
                min="0"
                placeholder="100.00"
                defaultValue={product?.minimumAmount}
                required
              />
              <Input
                label="Monto Máximo"
                name="maximumAmount"
                type="number"
                step="0.01"
                min="0"
                placeholder="10000.00"
                defaultValue={product?.maximumAmount}
                required
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Plazo Mínimo (días)"
                name="minimumTermDays"
                type="number"
                min="1"
                placeholder="1"
                defaultValue={product?.minimumTermDays}
                required
              />
              <Input
                label="Plazo Máximo (días)"
                name="maximumTermDays"
                type="number"
                min="1"
                placeholder="360"
                defaultValue={product?.maximumTermDays}
                required
              />
            </div>
          </div>
        </Card>

        {/* Fees */}
        <Card>
          <h2 className="text-lg font-semibold mb-4">Comisiones</h2>
          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Comisión (%)"
              name="commissionPercentage"
              type="number"
              step="0.01"
              min="0"
              max="100"
              placeholder="0"
              defaultValue={product?.commissionPercentage ?? 0}
              helperText="Porcentaje sobre el monto del préstamo"
            />
          </div>
        </Card>

        {/* Options */}
        <Card>
          <h2 className="text-lg font-semibold mb-4">Opciones</h2>
          <label className="flex items-center gap-3">
            <input
              type="checkbox"
              name="requiresGuarantor"
              defaultChecked={product?.requiresGuarantor ?? false}
              className="w-4 h-4 text-blue-600 rounded focus:ring-2"
            />
            <div>
              <p className="font-medium">Requiere Avalista</p>
              <p className="text-sm text-gray-600">
                El préstamo debe tener un avalista registrado
              </p>
            </div>
          </label>
        </Card>

        {/* Actions */}
        <Card>
          <div className="flex gap-3">
            <Button
              type="submit"
              disabled={createMutation.isPending || updateMutation.isPending}
            >
              {(createMutation.isPending || updateMutation.isPending)
                ? 'Guardando...'
                : isEditing
                ? 'Guardar Cambios'
                : 'Crear Producto'}
            </Button>
            <Button
              type="button"
              variant="secondary"
              onClick={() => navigate('/loan-products')}
            >
              Cancelar
            </Button>
          </div>
        </Card>
      </form>
      <ToastContainer toasts={toast.toasts} onClose={toast.close} />
    </div>
  );
}
