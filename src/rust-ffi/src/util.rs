#[repr(C)]
#[derive(Debug)]
pub struct RawVec<T> {
    pub size: usize,
    pub cap: usize,
    pub data: *mut T,
}

#[macro_export]
macro_rules! deref {
    ($ptr:ident) => {{
        assert!(!$ptr.is_null(), "null pointer {{stringify!($ptr)}}");
        unsafe { &*$ptr }
    }};
}
