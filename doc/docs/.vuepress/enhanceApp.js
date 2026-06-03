const SIDEBAR_FILTER_ID = 'air-cloud-sidebar-filter'
const SIDEBAR_FILTER_STORAGE_KEY = 'air-cloud-sidebar-filter-value'

const SIDEBAR_GROUPS = [
  { value: '0', label: '首页' },
  { value: '1', label: '设计理念' },
  { value: '2', label: '核心定位' },
  { value: '3', label: '开源信息' },
  { value: '4', label: '环境要求' },
  { value: '5', label: '基本概念' },
  { value: '6', label: '应用类型' },
  { value: '7', label: '加载机制' },
  { value: '8', label: '标准' },
  { value: '9', label: '模块' },
  { value: '10', label: '插件' },
  { value: '11', label: '配置文件' },
  { value: '12', label: 'Web服务' },
  { value: '13', label: '测试报告' },
  { value: '14', label: '使用建议' },
  { value: '15', label: '其他内容' },
  { value: '16', label: '技术学习' }
]

function createSidebarFilter () {
  const wrapper = document.createElement('div')
  wrapper.id = SIDEBAR_FILTER_ID
  wrapper.className = 'air-cloud-sidebar-filter'

  const select = document.createElement('select')
  select.id = `${SIDEBAR_FILTER_ID}-select`
  select.className = 'air-cloud-sidebar-filter__select'
  select.setAttribute('aria-label', '筛选侧边栏文档分类')

  const allOption = document.createElement('option')
  allOption.value = 'all'
  allOption.textContent = '全部分类'
  select.appendChild(allOption)

  SIDEBAR_GROUPS.forEach(group => {
    const option = document.createElement('option')
    option.value = group.value
    option.textContent = group.label
    select.appendChild(option)
  })

  const storedValue = window.localStorage.getItem(SIDEBAR_FILTER_STORAGE_KEY)
  select.value = SIDEBAR_GROUPS.some(group => group.value === storedValue) ? storedValue : 'all'
  select.addEventListener('change', () => {
    window.localStorage.setItem(SIDEBAR_FILTER_STORAGE_KEY, select.value)
    applySidebarFilter(select.value)
  })

  wrapper.appendChild(select)

  return wrapper
}

function getSidebarItems () {
  return Array.from(document.querySelectorAll('.sidebar .sidebar-links > li'))
}

function applySidebarFilter (value) {
  getSidebarItems().forEach((item, index) => {
    item.style.display = value === 'all' || value === String(index) ? '' : 'none'
  })
}

function mountSidebarFilter () {
  const sidebar = document.querySelector('.sidebar')
  if (!sidebar) return

  let filter = document.getElementById(SIDEBAR_FILTER_ID)
  if (!filter) {
    filter = createSidebarFilter()
    sidebar.insertBefore(filter, sidebar.firstChild)
  }

  const select = filter.querySelector('select')
  const selectedValue = select ? select.value : 'all'
  applySidebarFilter(selectedValue)
}

export default ({ Vue, isServer }) => {
  if (isServer) return

  Vue.mixin({
    mounted () {
      this.$nextTick(() => {
        window.setTimeout(mountSidebarFilter, 0)
      })
    },
    watch: {
      $route () {
        this.$nextTick(() => {
          window.setTimeout(mountSidebarFilter, 0)
        })
      }
    }
  })
}
