import { Injectable } from '@angular/core'
import { BehaviorSubject } from 'rxjs'

type TicketKey = string

@Injectable({
  providedIn: 'root',
})
export class TicketSharedDataService {
  private keySubject = new BehaviorSubject<{
    key: TicketKey
    type: string
  } | null>(null)
  public key$ = this.keySubject.asObservable()

  updateData(key: TicketKey, type: string) {
    const newData = { key: key, type: type }
    this.keySubject.next(newData)
  }
}


type TicketKey = string

@Injectable({
  providedIn: 'root',
})
export class TicketSharedDataService {
  private keySubject = new BehaviorSubject<{
    key: TicketKey
    type: string
  } | null>(null)
  public key$ = this.keySubject.asObservable()

  updateData(key: TicketKey, type: string) {
    const newData = { key: key, type: type }
    this.keySubject.next(newData)
  }
}


