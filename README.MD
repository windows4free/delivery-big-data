# DeliveryBigData

Simulación de un sistema de delivery (tipo Uber Eats/Rappi) construida para demostrar ingesta y procesamiento de datos a gran volumen en tiempo real, usando **Kafka**, **.NET** y **MongoDB**. El sistema genera pedidos aleatorios pero realistas, los procesa a través de un pipeline de streaming, y calcula en vivo el **índice de saturación** (pedidos activos vs. repartidores disponibles) por zona de una ciudad ficticia.

## Arquitectura

```
React (Generador)
   → POST a Delivery.Api
      → Api genera el pedido (catálogos de Delivery.Shared)
      → Api lo publica a Kafka (topic "pedidos", key = zona)

Kafka (topic particionado por zona, 6 particiones)
   → Delivery.Consumer (escucha de forma independiente)
      → limpia datos sucios / descarta duplicados
      → guarda en MongoDB (vía Delivery.Infraestructura)
      → recalcula saturación de esa zona en tiempo real

Delivery.Repartidores (Worker independiente, ciclo cada 15s)
   → simula disponibilidad de repartidores por zona
   → avanza el estado de los pedidos (Pendiente → Entregado)
   → lee/escribe en MongoDB (vía Delivery.Infraestructura)

React (Dashboard)
   → GET a Delivery.Api (/api/saturacion)
      → Api lee de MongoDB (vía Delivery.Infraestructura)
      → devuelve el balance de saturación por zona
```

Api, Consumer y Repartidores **no se llaman directamente entre sí** — se comunican de forma desacoplada a través de Kafka y de la base de datos compartida.

## Estructura del proyecto

```
DeliveryBigData/
├── DeliveryBigData.sln
├── Delivery.Shared/          → Modelos, interfaces y catálogos (zonas, restaurantes, categorías)
├── Delivery.Infraestructura/ → Repositorios MongoDB (Pedidos, Saturación)
├── Delivery.Api/             → Generador de pedidos + endpoints REST + productor Kafka
├── Delivery.Consumer/        → Worker: consume Kafka, limpia datos, guarda y calcula saturación
├── Delivery.Repartidores/    → Worker: simula repartidores y avance de estados de pedidos
└── delivery-dashboard/       → React + Vite (generador de pedidos + dashboard de saturación)
```

## Requisitos previos

- Visual Studio 2022 (o superior) con carga de trabajo .NET
- .NET SDK 10
- Node.js (para el dashboard en React)
- Docker Desktop (para correr Kafka)
- MongoDB Community Server

## Cómo levantar el proyecto

### 1. Levantar Kafka (Docker)

```bash
docker compose up -d
```

Crear el topic (una sola vez, si no existe todavía):

```bash
docker exec -it broker /opt/kafka/bin/kafka-topics.sh --bootstrap-server localhost:9092 --create --topic pedidos --partitions 6
```

### 2. Levantar MongoDB

```bash
cd "C:\Program Files\MongoDB\Server\7.0\bin"
mongod --dbpath C:\data\db
```

Dejar esa ventana abierta.

### 3. Levantar los servicios .NET

En Visual Studio: **Solución → Configurar proyectos de inicio → Varios proyectos de inicio**, y activar `Start` en:

- `Delivery.Api`
- `Delivery.Consumer`
- `Delivery.Repartidores`

Presionar `F5`. La Api debería quedar escuchando en `http://localhost:5199`.

> Opcional: para demostrar paralelismo, se puede correr una segunda instancia de `Delivery.Consumer` desde terminal:
> ```bash
> cd Delivery.Consumer\bin\Debug\net10.0
> dotnet Delivery.Consumer.dll
> ```
> Ambas instancias comparten el mismo `GroupId` de Kafka, que reparte las particiones automáticamente entre ellas.

### 4. Levantar el dashboard (React)

```bash
git clone 
git clone https://github.com/windows4free/delivery-dashboard.git
```

```bash
cd delivery-dashboard
npm install
npm run dev
```

Abrir `http://localhost:5173`.

## Uso

**Pestaña Generador**
- Botón individual: genera 1 pedido.
- Botones rápidos: simulan distintos volúmenes ("Día tranquilo", "Pico moderado", "Hora de almuerzo").
- Input personalizado: permite elegir la cantidad exacta de pedidos a enviar en lote.
- Cada envío por lote muestra el throughput medido (pedidos/segundo).

**Pestaña Dashboard**
- Selector de zona (6 zonas: Centro, Metropolitana, Norte, Sur, Este, Oeste).
- Número grande con el índice de saturación actual de la zona seleccionada.
- Tabla de historial (últimos 15 minutos).
- Vista general con tarjetas de las 6 zonas, coloreadas según nivel de saturación.

## Endpoints principales de la API

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/pedidos` | Genera y publica 1 pedido individual |
| POST | `/api/pedidos/lote?cantidad=N` | Genera y publica N pedidos, devuelve throughput |
| GET | `/api/pedidos/resumen-estados` | Cuenta pedidos por estado (Pendiente, En camino, Entregado, etc.) |
| GET | `/api/pedidos/hora-pico` | Indica si la hora actual corresponde a horario pico |
| GET | `/api/saturacion` | Saturación actual de las 6 zonas |
| GET | `/api/saturacion/{zona}` | Saturación actual de una zona específica |
| GET | `/api/saturacion/{zona}/historial?minutos=N` | Historial de saturación de una zona |

## Decisiones técnicas destacadas

- **Particionamiento por zona (Key = zona)** en un solo topic Kafka, en vez de un topic por zona, priorizando orden por zona y escalabilidad.
- **Limpieza de datos en 4 capas** en el Consumer: JSON inválido, campos nulos, zona inválida, monto inválido; además de detección de duplicados.
- **Saturación recalculada en tiempo real** ante cada evento nuevo, con historial persistido para poder observar tendencia.
- **Simulación de repartidores dinámicos** como su propio flujo de eventos (no un valor fijo), con un límite superior (`Math.Clamp`) para mantener un rango realista.
- **Sin HTTPS en local**: decisión consciente para un entorno de desarrollo/demo, priorizando medir throughput real sin el overhead de TLS.

## Autor
WILSON VARGAS
