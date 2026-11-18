import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ToastrService } from 'ngx-toastr';
import { vi } from 'vitest';
import { App } from './app';
import { appConfig } from './app.config';

describe('App', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [App],
      providers: [...appConfig.providers, provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('renders the meeting rooms page', () => {
    const fixture = TestBed.createComponent(App);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/rooms').flush([]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('h1')?.textContent).toContain('Meeting rooms');
    http.verify();
  });

  it('shows success toasts after creating and cancelling a booking', async () => {
    const fixture = TestBed.createComponent(App);
    const http = TestBed.inject(HttpTestingController);
    const page: HTMLElement = fixture.nativeElement;
    const expectSchedule = () =>
      http.expectOne(request => request.url === '/api/rooms/1/bookings');

    http.expectOne('/api/rooms').flush([{ id: 1, name: 'Room A' }]);
    const schedule = expectSchedule();
    const day = schedule.request.params.get('date');
    schedule.flush([]);
    fixture.detectChanges();

    const title = page.querySelector<HTMLInputElement>('#title')!;
    title.value = 'Team sync';
    title.dispatchEvent(new Event('input'));
    page.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    const request = http.expectOne({ method: 'POST', url: '/api/bookings' });
    const booking = {
      id: 7,
      roomId: 1,
      title: 'Team sync',
      startUtc: day + 'T09:00:00Z',
      endUtc: day + 'T10:00:00Z',
    };
    request.flush(booking, { status: 201, statusText: 'Created' });
    expectSchedule().flush([booking]);

    await vi.waitFor(() => {
      expect(document.querySelector('#toast-container')?.textContent).toContain('was booked.');
    });

    fixture.detectChanges();
    page.querySelector<HTMLButtonElement>('button.cancel')!.click();
    http.expectOne({ method: 'DELETE', url: '/api/bookings/7' })
      .flush(null, { status: 204, statusText: 'No Content' });
    expectSchedule().flush([]);

    await vi.waitFor(() => {
      expect(document.querySelector('#toast-container')?.textContent).toContain('was cancelled.');
    });
    TestBed.inject(ToastrService).clear();
    http.verify();
  });
});
