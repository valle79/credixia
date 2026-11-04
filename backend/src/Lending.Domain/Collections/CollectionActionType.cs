namespace Lending.Domain.Collections;

public enum CollectionActionType
{
    PhoneCall = 1,          // Llamada telefónica
    Visit = 2,              // Visita al cliente
    Message = 3,            // Mensaje SMS/WhatsApp
    Promise = 4,            // Promesa de pago
    LegalAction = 5         // Acción legal
}
