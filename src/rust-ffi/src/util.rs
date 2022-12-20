#[macro_export]
macro_rules! deref {
    ($ptr:ident) => {{
        assert!(!$ptr.is_null(), "null pointer {{stringify!($ptr)}}");
        unsafe { &*$ptr }
    }};
}
