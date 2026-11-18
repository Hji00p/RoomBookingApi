import { DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { Booking, BookingApi, Room, bookingError } from './booking-api';

@Component({
  selector: 'app-root',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly api = inject(BookingApi);
  private readonly destroyRef = inject(DestroyRef);
  private scheduleRequest?: Subscription;

  protected readonly rooms = signal<Room[]>([]);
  protected readonly roomId = signal(0);
  protected readonly date = signal(new Date().toISOString().slice(0, 10));
  protected readonly selectedRoom = computed(() => this.rooms().find(room => room.id === this.roomId()));
  protected readonly bookings = signal<Booking[]>([]);
  protected readonly roomsLoading = signal(false);
  protected readonly scheduleLoading = signal(false);
  protected readonly saving = signal(false);
  protected readonly cancellingId = signal<number | null>(null);
  protected readonly roomsError = signal('');
  protected readonly scheduleError = signal('');
  protected readonly formError = signal('');
  protected readonly cancellationError = signal('');
  protected readonly notice = signal('');

  protected readonly form = inject(FormBuilder).nonNullable.group({
    title: ['', Validators.required],
    start: ['09:00', Validators.required],
    end: ['10:00', Validators.required],
    overnight: false,
  });

  constructor() {
    this.loadRooms();
  }

  protected loadRooms(): void {
    this.roomsLoading.set(true);
    this.roomsError.set('');
    this.api.rooms().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: rooms => {
        this.roomsLoading.set(false);
        this.rooms.set(rooms);
        this.roomId.set(rooms[0]?.id ?? 0);
        this.loadSchedule();
      },
      error: error => {
        this.roomsLoading.set(false);
        this.roomsError.set(bookingError(error, 'Could not load rooms.'));
      },
    });
  }

  protected selectRoom(value: string): void {
    this.roomId.set(Number(value));
    this.selectionChanged();
  }

  protected selectDate(value: string): void {
    this.date.set(value);
    this.selectionChanged();
  }

  private selectionChanged(): void {
    this.notice.set('');
    this.cancellationError.set('');
    this.formError.set('');
    this.loadSchedule();
  }

  protected loadSchedule(): void {
    this.scheduleRequest?.unsubscribe();
    this.scheduleError.set('');
    this.bookings.set([]);
    this.scheduleLoading.set(false);
    if (!this.roomId() || !this.date()) return;

    this.scheduleLoading.set(true);
    this.scheduleRequest = this.api.schedule(this.roomId(), this.date())
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: bookings => {
          this.bookings.set(bookings);
          this.scheduleLoading.set(false);
        },
        error: error => {
          this.scheduleError.set(bookingError(error, 'Could not load the schedule.'));
          this.scheduleLoading.set(false);
        },
      });
  }

  protected createBooking(): void {
    if (this.saving() || !this.roomId() || !this.date()) return;
    this.formError.set('');
    this.notice.set('');
    const { title, start, end, overnight } = this.form.getRawValue();
    if (this.form.invalid || !title.trim()) {
      this.formError.set('Enter a meeting title and both times.');
      return;
    }

    const startTime = new Date(`${this.date()}T${start}:00Z`);
    const endTime = new Date(`${this.date()}T${end}:00Z`);
    if (overnight) endTime.setUTCDate(endTime.getUTCDate() + 1);
    if (!Number.isFinite(startTime.getTime()) || !Number.isFinite(endTime.getTime()) || endTime <= startTime) {
      this.formError.set('End time must be after start time.');
      return;
    }

    this.saving.set(true);
    this.api.create({
      roomId: this.roomId(),
      title: title.trim(),
      start: startTime.toISOString(),
      end: endTime.toISOString(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: booking => {
        this.saving.set(false);
        this.form.controls.title.reset();
        this.notice.set(`“${booking.title}” was booked.`);
        this.loadSchedule();
      },
      error: error => {
        this.saving.set(false);
        this.formError.set(bookingError(error, 'Could not create the booking.'));
      },
    });
  }

  protected cancelBooking(booking: Booking): void {
    if (this.cancellingId() !== null) return;
    this.notice.set('');
    this.cancellationError.set('');
    this.cancellingId.set(booking.id);
    this.api.cancel(booking.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.cancellingId.set(null);
        this.notice.set(`“${booking.title}” was cancelled.`);
        this.loadSchedule();
      },
      error: error => {
        this.cancellingId.set(null);
        this.cancellationError.set(bookingError(error, 'Could not cancel the booking.'));
      },
    });
  }
}
