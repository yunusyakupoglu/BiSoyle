import { PageTitleComponent } from '@/app/components/page-title.component'
import { CUSTOM_ELEMENTS_SCHEMA, Component } from '@angular/core'
import { SwiperOptions } from 'swiper/types'
import {
  Autoplay,
  EffectCreative,
  EffectFade,
  EffectFlip,
  Mousewheel,
  Navigation,
  Pagination,
  Scrollbar,
} from 'swiper/modules'
import { UIExamplesListComponent } from '@/app/components/ui-examples-list/ui-examples-list.component'
import { register } from 'swiper/element'
import { SwiperDirective } from '@/app/components/swiper-directive.component'

register()

@Component({
  selector: 'app-swiper',
  standalone: true,
  imports: [PageTitleComponent, UIExamplesListComponent, SwiperDirective],
  templateUrl: './swiper.component.html',
  styles: ``,
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class SwiperComponent {
  swiperConfig: SwiperOptions = {
    loop: true,
    autoplay: {
      delay: 2500,
      disableOnInteraction: false,
    },
  }

  swiperNavigation: SwiperOptions = {
    modules: [Autoplay, Pagination, Navigation],
    loop: true,
    autoplay: {
      delay: 2500,
      disableOnInteraction: false,
    },
    pagination: {
      clickable: true,
      el: '.basic-pagination',
    },
    navigation: {
      nextEl: '.basic-next',
      prevEl: '.basic-prev',
    },
  }

  swiperPagination: SwiperOptions = {
    modules: [Autoplay, Pagination],
    loop: true,
    autoplay: {
      delay: 2500,
      disableOnInteraction: false,
    },
    pagination: {
      clickable: true,
      el: '.dynamic-pagination',
      dynamicBullets: true,
    },
  }

  swiperfadeEffect: SwiperOptions = {
    modules: [Pagination, Autoplay, EffectFade],
    loop: true,
    effect: 'fade',
    autoplay: {
      delay: 2500,
      disableOnInteraction: false,
    },
    pagination: {
      clickable: true,
      el: '.effect-pagination',
    },
  }

  swiperCreativeEffect: SwiperOptions = {
    modules: [Autoplay, Pagination, EffectCreative],
    loop: true,
    grabCursor: true,
    effect: 'creative',
    // EffectCreative: {
    //     prev: {
    //         shadow: true,
    //         translate: [0, 0, -400],
    //     },
    //     next: {
    //         translate: ["100%", 0, 0],
    //     },
    // },
    autoplay: {
      delay: 2500,
      disableOnInteraction: false,
    },
    pagination: {
      el: '.creative-pagination',
      clickable: true,
    },
  }

  swiperFlip: SwiperOptions = {
    modules: [EffectFlip, Pagination],
    loop: true,
    effect: 'flip',
    grabCursor: true,
    autoplay: {
      delay: 2500,
      disableOnInteraction: false,
    },
    pagination: {
      el: '.swiper-flip',
      clickable: true,
    },
  }

  swiperScroll: SwiperOptions = {
    modules: [Autoplay, Scrollbar, Navigation],
    loop: true,
    autoplay: {
      delay: 2500,
      disableOnInteraction: false,
    },
    scrollbar: {
      el: '.swiper-scrollbar',
      hide: true,
    },
    navigation: {
      nextEl: '.swiper-button-next',
      prevEl: '.swiper-button-prev',
    },
  }

  verticalConfig: SwiperOptions = {
    modules: [Autoplay, Pagination],
    loop: true,
    direction: 'vertical',
    autoplay: {
      delay: 2500,
      disableOnInteraction: false,
    },
    pagination: {
      el: '.vertical-pagination',
      clickable: true,
    },
  }

  swiperMouseWheel: SwiperOptions = {
    modules: [Autoplay, Pagination, Mousewheel],
    loop: true,
    direction: 'vertical',
    mousewheel: true,
    autoplay: {
      delay: 2500,
      disableOnInteraction: false,
    },
    pagination: {
      el: '.mouse-wheel-pagination',
      clickable: true,
    },
  }

  resposiveConfig: SwiperOptions = {
    modules: [Pagination],
    loop: true,
    slidesPerView: 1,
    spaceBetween: 10,
    pagination: {
      el: '.responsive-pagination',
      clickable: true,
    },
    breakpoints: {
      768: {
        slidesPerView: 2,
        spaceBetween: 40,
      },
      1200: {
        slidesPerView: 3,
        spaceBetween: 50,
      },
    },
  }
}
