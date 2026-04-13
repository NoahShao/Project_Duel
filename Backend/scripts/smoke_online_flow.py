import argparse
import base64
import json
import os
import socket
import struct
import subprocess
import sys
import time
import urllib.request
from pathlib import Path


class WebSocketClient:
    def __init__(self, host: str, port: int, path: str) -> None:
        self._socket = socket.create_connection((host, port), timeout=5)
        self._socket.settimeout(5)
        websocket_key = base64.b64encode(os.urandom(16)).decode("ascii")
        request = (
            f"GET {path} HTTP/1.1\r\n"
            f"Host: {host}:{port}\r\n"
            "Upgrade: websocket\r\n"
            "Connection: Upgrade\r\n"
            f"Sec-WebSocket-Key: {websocket_key}\r\n"
            "Sec-WebSocket-Version: 13\r\n"
            "\r\n"
        )
        self._socket.sendall(request.encode("ascii"))
        response = b""
        while b"\r\n\r\n" not in response:
            chunk = self._socket.recv(4096)
            if not chunk:
                raise RuntimeError("WebSocket handshake failed: empty response.")
            response += chunk

        status_line = response.split(b"\r\n", 1)[0].decode("ascii", errors="replace")
        if "101" not in status_line:
            raise RuntimeError(f"WebSocket handshake failed: {status_line}")

    def send_json(self, payload: dict) -> None:
        encoded = json.dumps(payload, separators=(",", ":")).encode("utf-8")
        frame_header = self._build_header(len(encoded))
        mask = os.urandom(4)
        masked_payload = bytes(byte ^ mask[index % 4] for index, byte in enumerate(encoded))
        self._socket.sendall(frame_header + mask + masked_payload)

    def receive_json(self) -> dict:
        header = self._receive_exact(2)
        opcode = header[0] & 0x0F
        payload_length = header[1] & 0x7F
        is_masked = (header[1] & 0x80) != 0

        if payload_length == 126:
            payload_length = struct.unpack("!H", self._receive_exact(2))[0]
        elif payload_length == 127:
            payload_length = struct.unpack("!Q", self._receive_exact(8))[0]

        mask = self._receive_exact(4) if is_masked else b""
        payload = self._receive_exact(payload_length) if payload_length else b""
        if is_masked:
            payload = bytes(byte ^ mask[index % 4] for index, byte in enumerate(payload))

        if opcode == 0x8:
            return {"type": "__close__", "payload": {}}
        if opcode != 0x1:
            return {"type": f"__opcode_{opcode}__", "payload": {}}
        return json.loads(payload.decode("utf-8"))

    def close(self) -> None:
        try:
            self._socket.close()
        except OSError:
            pass

    def _build_header(self, payload_length: int) -> bytes:
        first_byte = 0x81
        mask_bit = 0x80
        if payload_length < 126:
            return bytes([first_byte, mask_bit | payload_length])
        if payload_length < 65536:
            return bytes([first_byte, mask_bit | 126]) + struct.pack("!H", payload_length)
        return bytes([first_byte, mask_bit | 127]) + struct.pack("!Q", payload_length)

    def _receive_exact(self, size: int) -> bytes:
        data = b""
        while len(data) < size:
            chunk = self._socket.recv(size - len(data))
            if not chunk:
                raise RuntimeError("Socket closed before frame was fully received.")
            data += chunk
        return data


def wait_for_health(url: str, timeout_seconds: float) -> dict:
    deadline = time.time() + timeout_seconds
    last_error: Exception | None = None
    while time.time() < deadline:
        try:
            with urllib.request.urlopen(url, timeout=2) as response:
                return json.loads(response.read().decode("utf-8"))
        except Exception as error:
            last_error = error
            time.sleep(0.3)

    raise RuntimeError(f"Health endpoint not ready: {last_error}")


def receive_until_types(client: WebSocketClient, expected_types: set[str], timeout_seconds: float) -> list[dict]:
    deadline = time.time() + timeout_seconds
    seen_types: set[str] = set()
    messages: list[dict] = []
    while time.time() < deadline and seen_types != expected_types:
        message = client.receive_json()
        messages.append(message)
        message_type = message.get("type")
        if message_type in expected_types:
            seen_types.add(message_type)

    if seen_types != expected_types:
        raise RuntimeError(f"Expected {sorted(expected_types)}, got {[item.get('type') for item in messages]}")
    return messages


def receive_initial_connected(client: WebSocketClient) -> None:
    message = client.receive_json()
    if message.get("type") != "connected":
        raise RuntimeError(f"Expected initial connected event, got {message}")


def acknowledge_hello(client: WebSocketClient, player_name: str) -> None:
    client.send_json({"type": "hello", "payload": {"playerName": player_name}})
    message = client.receive_json()
    if message.get("type") != "connected":
        raise RuntimeError(f"Expected hello ack for {player_name}, got {message}")


def decode_stream(data: bytes) -> str:
    return data.decode("utf-8", errors="replace").strip()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=5057)
    parser.add_argument("--path", default="/ws")
    args = parser.parse_args()

    backend_root = Path(__file__).resolve().parents[1]
    server_executable = backend_root / "ProjectDuel.Server" / "bin" / "Debug" / "net8.0" / "ProjectDuel.Server.exe"
    server_url = f"http://{args.host}:{args.port}"
    health_url = f"{server_url}/health"

    process = subprocess.Popen(
        [str(server_executable), "--urls", server_url],
        cwd=str(server_executable.parent),
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )

    clients: list[WebSocketClient] = []
    try:
        health_payload = wait_for_health(health_url, timeout_seconds=12)
        clients = [
            WebSocketClient(args.host, args.port, args.path),
            WebSocketClient(args.host, args.port, args.path),
        ]

        receive_initial_connected(clients[0])
        receive_initial_connected(clients[1])
        acknowledge_hello(clients[0], "Alice")
        acknowledge_hello(clients[1], "Bob")

        deck_a = {
            "deckId": "A",
            "displayName": "Deck A",
            "removedSuit": "",
            "cardIds": ["NO001", "NO002", "NO003"],
        }
        deck_b = {
            "deckId": "B",
            "displayName": "Deck B",
            "removedSuit": "",
            "cardIds": ["NO004", "NO005", "NO006"],
        }

        clients[0].send_json({"type": "create_room", "payload": {"playerName": "Alice", "deck": deck_a}})
        create_messages = receive_until_types(clients[0], {"room_created", "room_snapshot"}, timeout_seconds=5)
        room_id = next(message["payload"]["roomId"] for message in create_messages if message.get("type") == "room_created")

        clients[1].send_json({"type": "join_room", "payload": {"roomId": room_id, "playerName": "Bob", "deck": deck_b}})
        join_messages = receive_until_types(clients[1], {"room_joined", "room_snapshot"}, timeout_seconds=5)
        joined_room_id = next(message["payload"]["roomId"] for message in join_messages if message.get("type") == "room_joined")
        if joined_room_id != room_id:
            raise RuntimeError(f"Joined room mismatch: expected {room_id}, got {joined_room_id}")

        host_room_snapshot = clients[0].receive_json()
        if host_room_snapshot.get("type") != "room_snapshot":
            raise RuntimeError(f"Host expected room_snapshot after guest joined, got {host_room_snapshot}")
        if len(host_room_snapshot.get("payload", {}).get("players", [])) != 2:
            raise RuntimeError(f"Host room snapshot missing players: {host_room_snapshot}")

        clients[0].send_json({"type": "set_ready", "payload": {"isReady": True}})
        host_ready_snapshot = receive_until_types(clients[0], {"room_snapshot"}, timeout_seconds=5)[0]
        if not host_ready_snapshot["payload"]["players"][0]["isReady"]:
            raise RuntimeError("Host ready flag was not updated.")

        guest_ready_snapshot = clients[1].receive_json()
        if guest_ready_snapshot.get("type") != "room_snapshot":
            raise RuntimeError(f"Guest expected room_snapshot after host ready, got {guest_ready_snapshot}")

        clients[1].send_json({"type": "set_ready", "payload": {"isReady": True}})
        host_end_messages = [clients[0].receive_json() for _ in range(3)]
        guest_end_messages = [clients[1].receive_json() for _ in range(3)]

        for index, messages in enumerate([host_end_messages, guest_end_messages]):
            message_types = [message.get("type") for message in messages]
            if "match_started" not in message_types or "battle_snapshot" not in message_types:
                raise RuntimeError(f"Client {index} missing match_started/battle_snapshot: {message_types}")

        result = {
            "status": "ok",
            "health": health_payload,
            "roomId": room_id,
            "hostEvents": [message.get("type") for message in host_end_messages],
            "guestEvents": [message.get("type") for message in guest_end_messages],
        }
        print(json.dumps(result, ensure_ascii=False))
        return 0
    except Exception as error:
        stdout, stderr = process.communicate(timeout=3) if process.poll() is not None else (b"", b"")
        print(f"Smoke test failed: {error}", file=sys.stderr)
        if stdout:
            print("---server-stdout---", file=sys.stderr)
            print(decode_stream(stdout), file=sys.stderr)
        if stderr:
            print("---server-stderr---", file=sys.stderr)
            print(decode_stream(stderr), file=sys.stderr)
        return 1
    finally:
        for client in clients:
            client.close()

        if process.poll() is None:
            process.kill()
        try:
            process.communicate(timeout=3)
        except Exception:
            pass


if __name__ == "__main__":
    raise SystemExit(main())
