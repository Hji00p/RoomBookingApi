import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

export interface Room { id: number; name: string; }
export interface Booking {
  id: number;
  roomId: number;
  title: string;
  startUtc: string;
  endUtc: string;
}
export interface CreateBooking {
  roomId: number;
  title: string;
  start: string;
  end: string;
}

@Injectable({ providedIn: 'root' })
export class BookingApi {
  private readonly http = inject(HttpClient);

  rooms() {
    return this.http.get<Room[]>('/api/rooms');
  }

  schedule(roomId: number, date: string) {
    return this.http.get<Booking[]>(`/api/rooms/${roomId}/bookings`, { params: { date } });
  }

  create(booking: CreateBooking) {
    return this.http.post<Booking>('/api/bookings', booking);
  }

  cancel(id: number) {
    return this.http.delete<void>(`/api/bookings/${id}`);
  }
}

export function bookingError(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0 || error.status === 502 || error.status === 504) {
      return 'Could not reach the booking API. Check that it is running and try again.';
    }
    const problem = error.error;
    if (typeof problem?.detail === 'string') return problem.detail;
    const firstError = Object.values(problem?.errors ?? {}).flat()[0];
    if (typeof firstError === 'string') return firstError;
  }
  return fallback;
}
