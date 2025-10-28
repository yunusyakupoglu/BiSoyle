import { PageTitleComponent } from '@/app/components/page-title.component'
import { UIExamplesListComponent } from '@/app/components/ui-examples-list/ui-examples-list.component'
import { Component } from '@angular/core'
import { FormsModule } from '@angular/forms'
import { ClipboardModule, ClipboardService } from 'ngx-clipboard'
import { Subscription, take } from 'rxjs'

@Component({
  selector: 'app-clipboard',
  standalone: true,
  imports: [
    PageTitleComponent,
    ClipboardModule,
    FormsModule,
    UIExamplesListComponent,
  ],
  templateUrl: './clipboard.component.html',
  styles: ``,
})
export class ClipboardComponent {
  text1: string = 'name@example.com'
  text2: string =
    'Mussum ipsum cacilds, vidis litro abertis. Consetis adipiscings elitis. Pra lá , depois divoltis porris, paradis. Paisis, filhis, espiritis santis. Mé faiz elementum girarzis, nisi eros vermeio, in elementis mé pra quem é amistosis quis leo. Manduma pindureta quium dia nois paga.'
  isCopied1!: boolean
  isCopied2!: boolean
  isCopied3!: boolean
  copiedValue: string = ''

  private copySubscription: Subscription | null = null

  constructor(private _clipboardService: ClipboardService) {}

  callServiceToCopy1() {
    this.copySubscription = this._clipboardService.copyResponse$
      .pipe(take(1))
      .subscribe((re) => {
        if (re.isSuccess) {
          alert('Copied text:' + re.content)
        }
      })
  }

  callServiceToCopy() {
    this.copySubscription = this._clipboardService.copyResponse$
      .pipe(take(1))
      .subscribe((re) => {
        if (re.isSuccess) {
          alert('Copied text:' + re.content)
        }
      })
    this.text2 = ''
  }

  onCopyFailure() {
    alert('copy fail!')
  }
}
