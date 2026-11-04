import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { loanProductsApi } from '../api/loanProducts.api';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Badge } from '../components/ui/Badge';
import { Card } from '../components/ui/Card';
import { formatCurrency, formatPercent } from '../lib/format';
import { useToast } from '../hooks/useToast';
import { useAuth } from '../hooks/useAuth';
import { ToastContainer } from '../components/ui/ToastContainer';
import { getErrorMessage } from '../api/client';

export function LoanProductsPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const toast = useToast();
  const { tenant } = useAuth();
  const [searchTerm, setSearchTerm] = useState('');

  const { data: products, isLoading } = useQuery({
    queryKey: ['loanProducts', tenant?.id],
    queryFn: () => loanProductsApi.list(),
    enabled: !!tenant?.id,
  });

  const toggleStatusMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      loanProductsApi.updateStatus(id, isActive),
    onSuccess: (_, { isActive }) => {
      queryClient.invalidateQueries({ queryKey: ['loanProducts'] });
      toast.success(
        isActive ? 'Producto activado' : 'Producto desactivado',
        `El producto ha sido ${isActive ? 'activado' : 'desactivado'} exitosamente`
      );
    },
    onError: (error) => {
      toast.error('Error al cambiar estado', getErrorMessage(error));
    },
  });

  const getFrequencyLabel = (frequency: string) => {
    const labels: Record<string, string> = {
      Daily: 'Diario',
      Weekly: 'Semanal',
      Biweekly: 'Quincenal',
      Monthly: 'Mensual',
      Quarterly: 'Trimestral',
      Biannual: 'Semestral',
      Annual: 'Anual',
    };
    return labels[frequency] || frequency;
  };

  const getInterestTypeLabel = (type: string) => {
    const labels: Record<string, string> = {
      Simple: 'Simple',
      Compound: 'Compuesto',
    };
    return labels[type] || type;
  };

  const filteredProducts = (products ?? []).filter((product) => {
    const term = searchTerm.trim().toLowerCase();
    if (!term) {
      return true;
    }

    return (
      product.name.toLowerCase().includes(term) ||
      product.code.toLowerCase().includes(term)
    );
  });

  if (isLoading) {
    return <div className="p-6">Cargando...</div>;
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Productos de Préstamo</h1>
          <p className="text-gray-600">Gestiona los productos disponibles</p>
        </div>
        <Button onClick={() => navigate('/loan-products/new')}>
          + Nuevo Producto
        </Button>
      </div>

      {/* Search */}
      <Card>
        <Input
          type="search"
          placeholder="Buscar por nombre o código..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />
      </Card>

      {/* Products Grid */}
      {filteredProducts.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredProducts.map((product) => (
            <Card key={product.id} className="hover:shadow-lg transition-shadow">
              <div className="flex items-start justify-between mb-4">
                <div>
                  <h3 className="text-lg font-semibold">{product.name}</h3>
                  <p className="text-sm text-gray-600">{product.code}</p>
                </div>
                <Badge variant={product.isActive ? 'success' : 'default'}>
                  {product.isActive ? 'Activo' : 'Inactivo'}
                </Badge>
              </div>

              {product.description && (
                <p className="text-sm text-gray-700 mb-4">{product.description}</p>
              )}

              <div className="space-y-2 mb-4">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Frecuencia:</span>
                  <span className="font-medium">
                    {getFrequencyLabel(product.frequency)}
                  </span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Interés:</span>
                  <span className="font-medium">
                    {formatPercent(product.interestRate)} ({getInterestTypeLabel(product.interestType)})
                  </span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Monto mín/máx:</span>
                  <span className="font-medium">
                    {formatCurrency(product.minimumAmount, product.currency)} - {formatCurrency(product.maximumAmount, product.currency)}
                  </span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Plazo mín/máx:</span>
                  <span className="font-medium">
                    {product.minimumTermDays} - {product.maximumTermDays} días
                  </span>
                </div>
              </div>

              <div className="flex gap-2 pt-4 border-t">
                <Button
                  variant="secondary"
                  size="sm"
                  className="flex-1"
                  onClick={() => navigate(`/loan-products/${product.id}`)}
                >
                  Editar
                </Button>
                <Button
                  variant={product.isActive ? 'ghost' : 'primary'}
                  size="sm"
                  className="flex-1"
                  onClick={() =>
                    toggleStatusMutation.mutate({
                      id: product.id,
                      isActive: !product.isActive,
                    })
                  }
                  disabled={toggleStatusMutation.isPending}
                >
                  {product.isActive ? 'Desactivar' : 'Activar'}
                </Button>
              </div>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <div className="text-center py-12">
            <p className="text-gray-600 mb-4">
              {searchTerm
                ? 'No se encontraron productos'
                : 'No hay productos registrados'}
            </p>
            <Button onClick={() => navigate('/loan-products/new')}>
              Crear Primer Producto
            </Button>
          </div>
        </Card>
      )}
      <ToastContainer toasts={toast.toasts} onClose={toast.close} />
    </div>
  );
}
