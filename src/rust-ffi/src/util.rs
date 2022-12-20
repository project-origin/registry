#[repr(C)]
#[derive(Debug)]
pub struct RawVec {
    pub size: usize,
    pub cap: usize,
    pub ptr: *mut u8,
}

#[macro_export]
macro_rules! deref {
    ($ptr:ident) => {{
        assert!(!$ptr.is_null(), "null pointer {{stringify!($ptr)}}");
        unsafe { &*$ptr }
    }};
}
