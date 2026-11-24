// src/lib/websocket/useSignalR.ts
// Real-time SignalR hook for live application status updates

"use client";

import { useEffect, useRef, useState, useCallback } from "react";
import * as signalR from "@microsoft/signalr";

const HUB_URL = process.env.NEXT_PUBLIC_API_WS_URL ?? "ws://localhost:5000/hubs/notifications";

type ConnectionState = "disconnected" | "connecting" | "connected" | "error";

interface UseSignalROptions {
  onApplicationUpdate?: (data: { applicationId: string; status: string; stage: string }) => void;
  onNotification?: (data: { title: string; body: string; type: string }) => void;
  onTicketUpdate?: (data: { ticketId: string; status: string }) => void;
}

export function useSignalR(token: string | null, options: UseSignalROptions = {}) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [state, setState] = useState<ConnectionState>("disconnected");

  const connect = useCallback(async () => {
    if (!token || connectionRef.current) return;

    setState("connecting");

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => token,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Event listeners
    connection.on("ApplicationStatusChanged", options.onApplicationUpdate ?? (() => {}));
    connection.on("NewNotification", options.onNotification ?? (() => {}));
    connection.on("TicketUpdated", options.onTicketUpdate ?? (() => {}));

    connection.onreconnecting(() => setState("connecting"));
    connection.onreconnected(() => setState("connected"));
    connection.onclose(() => setState("disconnected"));

    try {
      await connection.start();
      connectionRef.current = connection;
      setState("connected");
    } catch {
      setState("error");
    }
  }, [token, options]);

  const disconnect = useCallback(async () => {
    if (connectionRef.current) {
      await connectionRef.current.stop();
      connectionRef.current = null;
      setState("disconnected");
    }
  }, []);

  useEffect(() => {
    connect();
    return () => {
      disconnect();
    };
  }, [connect, disconnect]);

  return { state, disconnect };
}
