#[repr(C)]
#[derive(Debug)]
pub struct RawVec<T> {
    pub data: *mut T,
    pub size: usize,
    pub cap: usize,
}

#[macro_export]
macro_rules! deref {
    ($ptr:ident) => {{
        assert!(!$ptr.is_null(), "null pointer");
        unsafe { &*$ptr }
    }};
}
